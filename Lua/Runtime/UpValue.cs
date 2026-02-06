using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lua.Runtime;

public sealed class UpValue
{
    public LuaValue Value;

    internal LuaState? Thread;

    public int RegisterIndex = 0;

    UpValue()
    {}

    public static UpValue Closed(LuaValue value)
        => new () { Value = value };

    public static UpValue Open(LuaState? thread, int registerIndex)
    {
        return new UpValue { Thread = thread, RegisterIndex = registerIndex };
    }

    public bool IsClosed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Thread == null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref LuaValue GetRefValue() => 
        ref Thread != null ? ref Thread!.stack[RegisterIndex] : ref Value;

    internal void Close()
    {
        Debug.Assert(IsClosed, "Cannot close an already closed upvalue.");
        Value = Thread!.stack[RegisterIndex];
        Thread = null;
    }
}
