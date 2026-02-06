using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lua.Internal;
using Lua.Runtime;

namespace Lua;

public sealed class LuaTable
{
    public LuaTable() : this(DefaultArrayCap, DefaultDictCap)
    {
    }

    public LuaTable(int arrayCapacity, int dictionaryCapacity)
    {
        array = new LuaValue[arrayCapacity];
        dict = new(dictionaryCapacity);
    }

    LuaValue[] array = [];
    internal LuaValueDictionary? dict;
    LuaTable? metatable;

    const int DefaultArrayCap = 8;
    const int DefaultDictCap = 8;

    private const int MaxArraySize = 1 << 24;
    private const int MaxDistance = 1 << 12;

    public LuaValue this[in LuaValue key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (key.type == LuaValueType.Nil) ThrowIndexIsNil();

            if (key.TryToInteger(out var index))
            {
                ulong _index = (ulong)(index - 1);
                if (_index < (ulong)array.Length) // Arrays in Lua are 1-origin...
                    //return array[_index];
                    return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)_index);
            }

            if (dict?.TryGetValue(key, out var value) == true) return value;
            return LuaValue.Nil;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (key.TryToInteger(out var index))
            {
                ulong _index = (ulong)(index - 1);
                if (_index < (ulong)array.Length) // Arrays in Lua are 1-origin...
                {
                    //array[_index] = value;
                    Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)_index) = value;
                    return;
                }
            }
            dict ??= new LuaValueDictionary(DefaultDictCap);
            dict[key] = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void DoGet(in LuaValue key, /*out*/ref LuaValue val)
    {
        if (key.TryToInteger(out var index))
        {
            ulong _index = (ulong)(index - 1);
            if (_index < (ulong)array.Length)
            { // Arrays in Lua are 1-origin...
                //ref var val1 = ref array[_index];
                ref var val1 = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)_index);
                if (val1.rvalue == null)
                {
                    val.type = val1.type;
                    val.ivalue = val1.ivalue;
                    val.rvalue = null;
                }
                else
                    val = val1;
                return;
            }
        }
        else if (key.IsNil) ThrowIndexIsNil();

        if (dict?.TryGetValue(key, out var value) == true) val = value;
        val = LuaValue.Nil;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void DoSet(int key, in LuaValue val)
    {
        uint _index = (uint)(key - 1); // Arrays in Lua are 1-origin...
        if (_index < (ulong)array.Length)
        {
            //ref var val1 = ref array[_index];
            ref var val1 = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)_index);
            if (val.rvalue == null)
            {
                val1.type = val.type;
                val1.ivalue = val.ivalue;
                val1.rvalue = null;
            }
            else
            {
                val1 = val;
            }
            return;
        }
        dict ??= new LuaValueDictionary(DefaultDictCap);
        dict[key] = val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void DoSet(in LuaValue key, in LuaValue val)
    {
        if (key.TryToInteger(out var index))
        {
            ulong _index = (ulong)(index - 1); // Arrays in Lua are 1-origin...
            if (_index < (ulong)array.Length)
            {
                //ref var val1 = ref array[_index];
                ref var val1 = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)_index);
                if (val.rvalue == null)
                {
                    val1.type = val.type;
                    val1.ivalue = val.ivalue;
                    val1.rvalue = null;
                }
                else
                {
                    val1 = val;
                }
                return;
            }
        }
        else if (key.IsNil) ThrowIndexIsNil();
        dict ??= new LuaValueDictionary(DefaultDictCap);
        dict[key] = val;
    }

    public LuaValue this[long key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ulong index = (ulong)(key - 1);
            if (index < (ulong)array.Length)
            {
                // Arrays in Lua are 1-origin...
                return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)index);
            }

            if (dict?.TryGetValue(key, out var value) == true) return value;
            return LuaValue.Nil;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ulong index = (ulong)(key - 1);
            if (index < (ulong)array.Length)
            {
                Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)index) = value;
                return;
            }
            dict ??= new LuaValueDictionary(DefaultDictCap);
            dict[key] = value;
        }
    }

    public long GetLength()
    {
        //find i such that array[i] != nil and array array[i+1] = nill
        int BSearch_Arr()
        {
            int left = 0;
            int right = array.Length - 1;
            while (left < right-1)
            {
                int mid = (left + right) / 2;
                if (!array[mid].IsNil)
                    left = mid;
                else
                    right = mid;
            }
            return left;
        }

        long BSearch_Map()
        {
            long left = array.Length + 1;
            long right = left + 1;
            if (dict[left].IsNil)
                return array.Length;
            while (!dict[right].IsNil)
            {
                if (right <= long.MaxValue / 2)
                    right *= 2;
                else
                {
                    right = long.MaxValue;
                    if (dict[right].IsNil)
                        break;
                    else
                        return right;
                }
            }
            while (left < right-1)
            {
                long mid = (left + right) / 2;
                if (!dict[mid].IsNil)
                    left = mid;
                else
                    right = mid;
            }
            return left;
        }
        if (array.Length > 0 && array[0].IsNil)
            return 0;
        else if (array.Length > 0 && array[array.Length - 1].IsNil)
            return BSearch_Arr() + 1;
        else if (dict == null)
            return array.Length;
        else
            return BSearch_Map();
    }

    public LuaTable? Metatable
    {
        get => metatable;
        set => metatable = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(LuaValue key, out LuaValue value)
    {
        if (key.type is LuaValueType.Nil)
        {
            value = default;
            return false;
        }

        if (key.TryToInteger(out var index))
        {
            if (index > 0 && index <= array.Length)
            {
                value = array[index - 1];
                return value.type is not LuaValueType.Nil;
            }
        }

        return dict.TryGetValue(key, out value) && value.type is not LuaValueType.Nil;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref LuaValue FindValue(LuaValue key)
    {
        if (key.type is LuaValueType.Nil)
        {
            ThrowIndexIsNil();
        }

        if (key.TryToInteger(out var index))
        {
            if (index > 0 && index <= array.Length)
            {
                return ref array[index - 1];
            }
        }

        return ref dict.FindValue(key, out _);
    }

    public bool ContainsKey(LuaValue key)
    {
        if (key.type is LuaValueType.Nil)
        {
            return false;
        }

        if (key.TryToInteger(out var index))
        {
            return index > 0 && index <= array.Length &&
                   array[index - 1].type != LuaValueType.Nil;
        }

        return dict.TryGetValue(key, out var value) && value.type is not LuaValueType.Nil;
    }

    public LuaValue RemoveAt(int index)
    {
        var arrayIndex = index - 1;
        var value = array[arrayIndex];

        if (arrayIndex < array.Length - 1)
        {
            array.AsSpan(arrayIndex + 1).CopyTo(array.AsSpan(arrayIndex));
        }

        array[^1] = default;

        return value;
    }

    public void Insert(int index, LuaValue value)
    {
        if (index <= 0 || index > array.Length + 1)
        {
            throw new IndexOutOfRangeException();
        }

        var arrayIndex = index - 1;
        var distance = index - array.Length;
        if (distance > MaxDistance)
        {
            dict[index] = value;
            return;
        }

        if (index > array.Length || array[^1].type != LuaValueType.Nil)
        {
            EnsureArrayCapacity(array.Length + 1);
        }

        if (arrayIndex != array.Length - 1)
        {
            array.AsSpan(arrayIndex, array.Length - arrayIndex - 1).CopyTo(array.AsSpan(arrayIndex + 1));
        }

        array[arrayIndex] = value;
    }

    public bool TryGetNext(LuaValue key, out KeyValuePair<LuaValue, LuaValue> pair)
    {
        var index = -1;
        if (key.type is LuaValueType.Nil)
        {
            index = 0;
        }
        else if (key.TryToInteger(out var integer) && integer > 0 && integer <= array.Length)
        {
            index = (int)integer;
        }

        if (index != -1)
        {
            var span = array.AsSpan(index);
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i].type is not LuaValueType.Nil)
                {
                    pair = new(index + i + 1, span[i]);
                    return true;
                }
            }

            foreach (var kv in dict)
            {
                if (kv.Value.type is not LuaValueType.Nil)
                {
                    pair = kv;
                    return true;
                }
            }
        }
        else
        {
            if (dict.TryGetNext(key, out pair))
            {
                return true;
            }
        }

        pair = default;
        return false;
    }

    public void Clear()
    {
        dict.Clear();
    }

    public Memory<LuaValue> GetArrayMemory()
    {
        return array.AsMemory();
    }

    public Span<LuaValue> GetArraySpan()
    {
        return array.AsSpan();
    }

    internal void EnsureArrayCapacity(int newCapacity)
    {
        if (array.Length >= newCapacity) return;

        var prevLength = array.Length;
        var newLength = array.Length;
        if (newLength == 0) newLength = 8;
        newLength = newCapacity <= 8 ? 8 : MathEx.NextPowerOfTwo(newCapacity);

        Array.Resize(ref array, newLength);

        using var indexList = new PooledList<(int, LuaValue)>(dict.Count);

        // Move some of the elements of the hash part to a newly allocated array
        foreach (var kv in dict)
        {
            if (kv.Key.TryToInteger(out var index))
            {
                if (index > prevLength && index <= newLength)
                {
                    indexList.Add(((int)index, kv.Value));
                }
            }
        }

        foreach ((var index, var value) in indexList.AsSpan())
        {
            dict.Remove(index);
            array[index - 1] = value;
        }
    }

    static void ThrowIndexIsNil()
    {
        throw new ArgumentException("the table index is nil");
    }

    static void ThrowIndexIsNaN()
    {
        throw new ArgumentException("the table index is NaN");
    }
}