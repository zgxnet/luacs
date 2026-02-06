using System.Runtime.CompilerServices;
using Lua.Internal;

namespace Lua.Runtime;

public sealed class LuaClosure
{
    internal Proto proto;
    internal UpValue[] upValues = [];
    
    public LuaClosure(LuaState thread, Proto proto)
    {
        this.proto = proto;
        var upValueDescs = proto.UpValues;
        upValues = new UpValue[upValueDescs.Length];

        // add upvalues
        for (int i = 0; i < upValueDescs.Length; i++)
        {
            var description = proto.UpValues[i];
            upValues[i] = GetUpValueFromDescription(thread, description);
        }
    }

    public Proto Proto => proto;
    public ReadOnlySpan<UpValue> UpValues => upValues.AsSpan();
    internal Span<UpValue> GetUpValuesSpan() => upValues.AsSpan();

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //internal LuaValue GetUpValue(int index)
    //{
    //    return upValues[index].GetValue();
    //}

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //internal ref readonly LuaValue GetUpValueRef(int index)
    //{
    //    return ref upValues[index].GetValueRef();
    //}

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //internal void SetUpValue(int index, LuaValue value)
    //{
    //    upValues[index].SetValue(value);
    //}

    static UpValue GetUpValueFromDescription(LuaState thread, UpValueDesc description) //0: _Env
    {
        ref readonly var frame = ref thread.GetCurrentFrame();
        if (description.InStack)
            return thread.GetOrAddUpValue(frame.func + description.Index);
        else
        {
            var closure = thread.stack[frame.func].AsLuaClosure();
            return closure.upValues[description.Index];
        }
        throw new LuaException($"Invalid upvalue description: {description}, function: {frame.func}");
    }
}
