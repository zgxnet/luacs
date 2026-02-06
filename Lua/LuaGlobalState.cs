using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Lua.Internal;
using Lua.Loaders;
using Lua.Runtime;
using Lua.Std;

namespace Lua;

public sealed class LuaGlobalState
{
    public const string DefaultChunkName = "chunk";

    // states
    readonly LuaState mainThread = new();
    //FastStackCore<LuaThread> threadStack;
    readonly LuaTable packages = new();
    readonly LuaTable environment;
    readonly LuaTable registry = new();
    readonly UpValue envUpValue;
    bool isRunning;

    //FastStackCore<LuaDebug.LuaDebugBuffer> debugBufferPool;

    internal UpValue EnvUpValue => envUpValue;
    //internal ref FastStackCore<LuaThread> ThreadStack => ref threadStack;
    //internal ref FastStackCore<LuaDebug.LuaDebugBuffer> DebugBufferPool => ref debugBufferPool;

    public LuaTable Environment => environment;
    public LuaTable Registry => registry;
    public LuaTable LoadedModules => packages;
    public LuaState MainThread => mainThread;
    //public LuaThread CurrentThread
    //{
    //    get
    //    {
    //        if (threadStack.TryPeek(out var thread)) return thread;
    //        return mainThread;
    //    }
    //}

    public ILuaModuleLoader ModuleLoader { get; set; } = FileModuleLoader.Instance;

    // metatables
    LuaTable? nilMetatable;
    LuaTable? numberMetatable;
    LuaTable? stringMetatable;
    LuaTable? booleanMetatable;
    LuaTable? functionMetatable;
    LuaTable? threadMetatable;

    public LuaGlobalState()
    {
        environment = new();
        envUpValue = UpValue.Closed(environment);
        mainThread.globalState = this;
        mainThread.callStack.APush(new CallInfo
        {
            func = -1,
            top = LuaConstants.LuaMinStack,
            savedpc = -1,

            nresults = 0,
            nextraargs = 0
        });
        mainThread.stack.ReserveMore(LuaConstants.LuaMinStack);
    }

    //public async ValueTask<int> RunAsync(Proto chunk, Memory<LuaValue> buffer, CancellationToken cancellationToken = default)
    //{
    //    ThrowIfRunning();

    //    Volatile.Write(ref isRunning, true);
    //    try
    //    {
    //        var closure = new LuaClosure(CurrentThread, chunk);
    //        return await closure.InvokeAsync(new()
    //        {
    //            State = this,
    //            Thread = CurrentThread,
    //            ArgumentCount = 0,
    //            FrameBase = 0,
    //            SourcePosition = null,
    //            RootChunkName = chunk.Name,
    //            ChunkName = chunk.Name,
    //        }, buffer, cancellationToken);
    //    }
    //    finally
    //    {
    //        Volatile.Write(ref isRunning, false);
    //    }
    //}

    //public void Push(LuaValue value)
    //{
    //    CurrentThread.stack.Push(value);
    //}

    public Traceback GetTraceback()
    {
        throw new NotImplementedException();
    //    if (threadStack.Count == 0)
    //    {
    //        return new(this)
    //        {
    //            RootFunc = (LuaClosure)MainThread.GetCallStackFrames()[0].Function,
    //            StackFrames = MainThread.GetCallStackFrames()[1..]
    //                .ToArray()
    //        };
    //    }

    //    using var list = new PooledList<CallStackFrame>(8);
    //    foreach (var frame in MainThread.GetCallStackFrames()[1..])
    //    {
    //        list.Add(frame);
    //    }

    //    foreach (var thread in threadStack.AsSpan())
    //    {
    //        if (thread.CallStack.Count == 0) continue;
    //        foreach (var frame in thread.GetCallStackFrames()[1..])
    //        {
    //            list.Add(frame);
    //        }
    //    }

    //    return new(this)
    //    {
    //        RootFunc = (LuaClosure)MainThread.GetCallStackFrames()[0].Function,
    //        StackFrames = list.AsSpan().ToArray()
    //    };
    }

    internal Traceback GetTraceback(LuaState thread)
    {
        throw new NotImplementedException();
        //using var list = new PooledList<CallStackFrame>(8);
        //foreach (var frame in thread.GetCallStackFrames()[1..])
        //{
        //    list.Add(frame);
        //}
        //LuaClosure rootFunc;
        //if (thread.GetCallStackFrames()[0].Function is LuaClosure closure)
        //{
        //    rootFunc = closure;
        //}
        //else
        //{
        //    rootFunc = (LuaClosure)MainThread.GetCallStackFrames()[0].Function;
        //}

        //return new(this)
        //{
        //    RootFunc = rootFunc,
        //    StackFrames = list.AsSpan().ToArray()
        //};
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetMetatable(LuaValue value, [NotNullWhen(true)] out LuaTable? result)
    {
        result = value.type switch
        {
            LuaValueType.Nil => nilMetatable,
            LuaValueType.Boolean => booleanMetatable,
            LuaValueType.String => stringMetatable,
            LuaValueType.Float => numberMetatable,
            LuaValueType.Function => functionMetatable,
            LuaValueType.Thread => threadMetatable,
            LuaValueType.UserData => value.UnsafeRead<ILuaUserData>().Metatable,
            LuaValueType.Table => value.UnsafeRead<LuaTable>().Metatable,
            _ => null
        };

        return result != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetMetatable(LuaValue value, LuaTable metatable)
    {
        switch (value.type)
        {
            case LuaValueType.Nil:
                nilMetatable = metatable;
                break;
            case LuaValueType.Boolean:
                booleanMetatable = metatable;
                break;
            case LuaValueType.String:
                stringMetatable = metatable;
                break;
            case LuaValueType.Float:
                numberMetatable = metatable;
                break;
            case LuaValueType.Function:
                functionMetatable = metatable;
                break;
            case LuaValueType.Thread:
                threadMetatable = metatable;
                break;
            case LuaValueType.UserData:
                value.UnsafeRead<ILuaUserData>().Metatable = metatable;
                break;
            case LuaValueType.Table:
                value.UnsafeRead<LuaTable>().Metatable = metatable;
                break;
        }
    }

    void ThrowIfRunning()
    {
        if (Volatile.Read(ref isRunning))
        {
            throw new InvalidOperationException("the lua state is currently running");
        }
    }

    public void OpenLibs()
    {
        Basic.Register(environment);
    }
}
