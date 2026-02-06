using System.Runtime.CompilerServices;
using Lua.Compiler;
using Lua.Internal;
using Lua.Runtime;

namespace Lua;

public delegate int LuaCFunction(LuaState L);
public delegate ValueTask<int> LuaAsyncFunction(LuaState L);

public class LuaState
{
    //public abstract LuaThreadStatus GetStatus();
    //public abstract void UnsafeSetStatus(LuaThreadStatus status);
    //public abstract ValueTask<int> ResumeAsync(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken = default);
    //public abstract ValueTask<int> YieldAsync(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken = default);

    internal FastStackCore<LuaValue> stack = new();
    internal FastStackCore<CallInfo> callStack = new();
    internal FastListCore<UpValue> openUpValues;
    internal LuaGlobalState globalState;

    internal bool IsLineHookEnabled
    {
        get => LineAndCountHookMask.Flag0;
        set => LineAndCountHookMask.Flag0 = value;
    }

    internal bool IsCountHookEnabled
    {
        get => LineAndCountHookMask.Flag1;
        set => LineAndCountHookMask.Flag1 = value;
    }

    internal BitFlags2 LineAndCountHookMask;

    internal bool IsCallHookEnabled
    {
        get => CallOrReturnHookMask.Flag0;
        set => CallOrReturnHookMask.Flag0 = value;
    }

    internal bool IsReturnHookEnabled
    {
        get => CallOrReturnHookMask.Flag1;
        set => CallOrReturnHookMask.Flag1 = value;
    }

    internal BitFlags2 CallOrReturnHookMask;
    internal bool IsInHook;
    internal int HookCount;
    internal int BaseHookCount;

    //internal LuaFunction? Hook { get; set; }

    internal LuaState()
    {}

    internal ref readonly CallInfo GetCurrentFrame()
        => ref callStack.PeekRef();

    public ReadOnlySpan<LuaValue> GetStackValues()
        => stack.AsSpan();

    internal ReadOnlySpan<CallInfo> GetCallStackFrames()
        => callStack.AsSpan();

    //c api
    ref struct CApiContext
    {
        public ref FastStackCore<LuaValue> stack;
        public ref CallInfo ci;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckOverflow()
        {
            if (stack.top >= ci.top)
                throw new InvalidOperationException("Stack overflow: too many values pushed onto the stack");
        }

        public void PushValue(int index)
        {
            CheckOverflow();
            stack.Push(stack[GetStackIndex(index)]);
        }

        public void PushValue(LuaValue value)
        {
            CheckOverflow();
            stack.Push(value);
        }

        public void PushNil() => PushValue(LuaValue.Nil);

        public void PushNumber(double number) => PushValue(new LuaValue(number));

        public void PushNumber(long number) => PushValue(new LuaValue(number));

        public void PushBoolean(bool value) => PushValue(new LuaValue(value));

        public void PushString(string str) => PushValue(new LuaValue(str));

        public void PushFunction(LuaCFunction func) => PushValue(new LuaValue(func));

        public int GetTop() => stack.top - ci.func - 1;

        public void SetTop(int index)
        {
            int top = GetTop();
            if (index < 0)
                index = top + index + 1; // convert to 1-based index
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Invalid index");
            int newTop = ci.func + index;
            stack.SetSize(newTop);
            ci.top = index;
        }

        public void Pop(int n) => SetTop(-n - 1);

        public LuaValue GetValue(int index) => stack[GetStackIndex(index)];

        public void SetField(int index, string k) //lua_setfield 
        {
            int tableIndex = GetStackIndex(index);
            int valueIndex = GetStackIndex(-1);
            LuaValue table = stack[tableIndex];
            if (!table.IsTable)
                throw new InvalidOperationException($"Cannot set field on non-table value at index {index}");
            table.AsTable()[k] = stack[valueIndex];
            Pop(1); // pop the table and the value
        }

        int GetStackIndex(int index)
        {
            int top = GetTop();
            if (index == 0)
                throw new ArgumentException("invalid index 0");
            else if (index < 0)
                index = top + index + 1; // convert to 1-based index
            if (index < 1 || index > top)
                throw new IndexOutOfRangeException($"Index {index} is out of range for stack with count {top}");
            return ci.func + index;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    CApiContext GetApiContext()
        => new CApiContext { ci = ref callStack.GetTopRef(), stack = ref stack };

    public void PushValue(int index) => GetApiContext().PushValue(index);

    public void PushValue(LuaValue val) => GetApiContext().PushValue(val);

    public int GetTop() => GetApiContext().GetTop();

    public void SetTop(int index) => GetApiContext().SetTop(index);

    public void Pop(int n) => GetApiContext().Pop(n);

    public LuaValue GetValue(int index) => GetApiContext().GetValue(index);

    public int LoadSourceFile(string fname, string? chunkname = null)
    {
        chunkname ??= Path.GetFileName(fname);
        byte[] data = ExternalCompiler.Compile(File.ReadAllText(fname));
        return LoadBinary(data, chunkname);
    }

    public int LoadBinary(byte[] data, string chunkname)
    {
        Proto proto = ProtoIO.LoadProto(data);
        return LoadProto(proto);
    }

    public int LoadFile(string fname, string chunkname)
    {
        Proto proto = ProtoIO.LoadProto(fname);
        return LoadProto(proto);
    }

    int LoadProto(Proto proto)
    {
        LuaClosure closure = new LuaClosure(this, proto);
        closure.upValues[0] = globalState.EnvUpValue;
        PushValue(new LuaValue(closure));
        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PopCallStackFrameUnsafe(int frameBase)
    {
        if (callStack.TryPop())
        {
            stack.PopUntil(frameBase);
        }
        else
        {
            ThrowForEmptyStack();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PopCallStackFrameUnsafe()
    {
        if (!callStack.TryPop())
        {
            ThrowForEmptyStack();
        }
    }

    internal void DumpStackValues()
    {
        var span = GetStackValues();
        for (int i = 0; i < span.Length; i++)
        {
            Console.WriteLine($"LuaStack [{i}]\t{span[i]}");
        }
    }

    static void ThrowForEmptyStack() => throw new InvalidOperationException("Empty stack");

    internal UpValue GetOrAddUpValue(int registerIndex)
    {
        foreach (var upValue in openUpValues.AsSpan()) //todo: binary search
        {
            if (upValue.RegisterIndex == registerIndex)
            {
                return upValue;
            }
        }

        var newUpValue = UpValue.Open(this, registerIndex);
        openUpValues.Add(newUpValue);
        return newUpValue;
    }

    internal void CloseUpValues(int frameBase)
    {
        Span<UpValue> opens = openUpValues.AsSpan();
        int p = opens.Length - 1;
        while (p >= 0 && opens[p].RegisterIndex >= frameBase)
            opens[p--].Close();
        openUpValues.Shrink(p + 1);
    }
}
