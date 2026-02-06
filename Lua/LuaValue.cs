using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lua.Internal;
using Lua.Runtime;

namespace Lua;

[StructLayout(LayoutKind.Explicit)]
public struct LuaValue : IEquatable<LuaValue>
{
    public static readonly LuaValue Nil = default;

    [FieldOffset(0)]
    internal LuaValueType type;

    [FieldOffset(8)]
    internal double fvalue;

    [FieldOffset(8)]
    internal long ivalue;

    [FieldOffset(8)]
    internal bool bvalue;

    [FieldOffset(16)]
    internal object? rvalue;

    public LuaValueType Type => type;

    public bool TryRead<T>(out T result)
    {
        var t = typeof(T);

        switch (type)
        {
            case LuaValueType.Float:
                if (t == typeof(float))
                {
                    var v = (float)fvalue;
                    result = Unsafe.As<float, T>(ref v);
                    return true;
                }
                else if (t == typeof(double))
                {
                    var v = fvalue;
                    result = Unsafe.As<double, T>(ref v);
                    return true;
                }
                else if (t == typeof(int))
                {
                    if (!MathEx.IsInteger(fvalue)) break;
                    var v = (int)fvalue;
                    result = Unsafe.As<int, T>(ref v);
                    return true;
                }
                else if (t == typeof(long))
                {
                    if (!MathEx.IsInteger(fvalue)) break;
                    var v = (long)fvalue;
                    result = Unsafe.As<long, T>(ref v);
                    return true;
                }
                else if (t == typeof(object))
                {
                    result = (T)(object)fvalue;
                    return true;
                }
                else
                {
                    break;
                }
            case LuaValueType.Integer:
                if (t == typeof(float))
                {
                    var v = (float)ivalue;
                    result = Unsafe.As<float, T>(ref v);
                    return true;
                }
                else if (t == typeof(double))
                {
                    var v = (double)ivalue;
                    result = Unsafe.As<double, T>(ref v);
                    return true;
                }
                else if (t == typeof(int))
                {
                    var v = (int)ivalue;
                    result = Unsafe.As<int, T>(ref v);
                    return true;
                }
                else if (t == typeof(long))
                {
                    var v = ivalue;
                    result = Unsafe.As<long, T>(ref v);
                    return true;
                }
                else if (t == typeof(object))
                {
                    result = (T)(object)ivalue;
                    return true;
                }
                else
                {
                    break;
                }
            case LuaValueType.Boolean:
                if (t == typeof(bool))
                {
                    var v = bvalue;
                    result = Unsafe.As<bool, T>(ref v);
                    return true;
                }
                else if (t == typeof(object))
                {
                    result = (T)(object)true;
                    return true;
                }
                else
                {
                    break;
                }
            case LuaValueType.String:
                if (t == typeof(string))
                {
                    var v = rvalue!;
                    result = Unsafe.As<object, T>(ref v);
                    return true;
                }
                else if (t == typeof(double))
                {
                    result = default!;
                    return TryParseToDouble(out Unsafe.As<T, double>(ref result));
                }
                else if (t == typeof(object))
                {
                    result = (T)rvalue!;
                    return true;
                }
                else
                {
                    break;
                }
            //case LuaValueType.Function: //todo
            //    if (t == typeof(LuaFunction) || t.IsSubclassOf(typeof(LuaFunction)))
            //    {
            //        var v = rvalue!;
            //        result = Unsafe.As<object, T>(ref v);
            //        return true;
            //    }
            //    else if (t == typeof(object))
            //    {
            //        result = (T)rvalue!;
            //        return true;
            //    }
            //    else
            //    {
            //        break;
            //    }
            case LuaValueType.Thread:
                if (t == typeof(LuaState))
                {
                    var v = rvalue!;
                    result = Unsafe.As<object, T>(ref v);
                    return true;
                }
                else if (t == typeof(object))
                {
                    result = (T)rvalue!;
                    return true;
                }
                else
                {
                    break;
                }
            case LuaValueType.LightUserData:
                {
                    if (rvalue is T tValue)
                    {
                        result = tValue;
                        return true;
                    }
                    break;
                }
            case LuaValueType.UserData:
                if (t == typeof(ILuaUserData) || typeof(ILuaUserData).IsAssignableFrom(t))
                {
                    if (rvalue is T tValue)
                    {
                        result = tValue;
                        return true;
                    }

                    break;
                }
                else if (t == typeof(object))
                {
                    result = (T)rvalue!;
                    return true;
                }
                else
                {
                    break;
                }
            case LuaValueType.Table:
                if (t == typeof(LuaTable))
                {
                    var v = rvalue!;
                    result = Unsafe.As<object, T>(ref v);
                    return true;
                }
                else if (t == typeof(object))
                {
                    result = (T)rvalue!;
                    return true;
                }
                else
                {
                    break;
                }
        }

        result = default!;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly bool TryToBool(out bool result)
    {
        if (type == LuaValueType.Boolean)
        {
            result = bvalue;
            return true;
        }
        result = false;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly bool TryToNumber(out double result)
    {
        if (type == LuaValueType.Float)
        {
            result = fvalue;
            return true;
        }else if(type == LuaValueType.Integer)
        {
            result = ivalue;
            return true;
        }
        result = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly bool TryToTable(out LuaTable result)
    {
        if (type == LuaValueType.Table)
        {
            var v = rvalue!;
            result = Unsafe.As<object, LuaTable>(ref v);
            return true;
        }

        result = default!;
        return false;
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //internal bool TryReadFunction(out LuaFunction result)
    //{
    //    if (Type == LuaValueType.Function)
    //    {
    //        var v = rvalue!;
    //        result = Unsafe.As<object, LuaFunction>(ref v);
    //        return true;
    //    }

    //    result = default!;
    //    return false;
    //}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]    
    internal bool TryReadString([NotNullWhen(true)] out string result)
    {
        if (type == LuaValueType.String)
        {
            var v = rvalue!;
            result = Unsafe.As<object, string>(ref v);
            return true;
        }

        result = default!;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryToInteger(out long result)
    {
        if (type == LuaValueType.Integer)
        {
            result = ivalue;
            return true;
        }
        else if (type == LuaValueType.Float)
        {
            result = (long)fvalue;
            if(result != fvalue)
            {
                result = 0;
                return false;
            }
            return true;
        }
        result = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryReadOrSetDouble(ref LuaValue luaValue, out double result)
    {
        if (luaValue.type == LuaValueType.Float)
        {
            result = luaValue.fvalue;
            return true;
        }

        if (luaValue.TryParseToDouble(out result))
        {
            luaValue = result;
            return true;
        }

        return false;
    }

    public readonly bool IsNil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => type == LuaValueType.Nil;
    }

    public readonly bool IsBool
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => type == LuaValueType.Boolean;
    }

    public readonly bool IsString
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => type == LuaValueType.String;
    }

    public readonly bool IsTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => type == LuaValueType.Table;
    }

    public readonly bool IsNumber
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => type == LuaValueType.Float || type == LuaValueType.Integer;
    }

    public readonly bool IsInteger
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => type == LuaValueType.Integer;
    }

    public readonly bool IsFloat
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => type == LuaValueType.Float;
    }

    public readonly bool IsCFunction
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => type == LuaValueType.CFunction;
    }

    public readonly bool IsLuaClosure
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => type == LuaValueType.Function;
    }

    bool TryParseToDouble(out double result)
    {
        if (type != LuaValueType.String)
        {
            result = default!;
            return false;
        }
        var str = Unsafe.As<string>(rvalue!);
        var span = str.AsSpan().Trim();
        if (span.Length == 0)
        {
            result = default!;
            return false;
        }

        var sign = 1;
        var first = span[0];
        if (first is '+')
        {
            sign = 1;
            span = span[1..];
        }
        else if (first is '-')
        {
            sign = -1;
            span = span[1..];
        }

        if (span.Length > 2 && span[0] is '0' && span[1] is 'x' or 'X')
        {
            // TODO: optimize
            try
            {
                var d = HexConverter.ToDouble(span) * sign;
                result = d;
                return true;
            }
            catch (FormatException)
            {
                result = default!;
                return false;
            }
        }
        else
        {
            return double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
    }

    public T Read<T>()
    {
        if (!TryRead<T>(out var result)) throw new InvalidOperationException($"Cannot convert LuaValueType.{type} to {typeof(T).FullName}.");
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T UnsafeRead<T>()
    {
        switch (type)
        {
            case LuaValueType.Boolean:
                {
                    var v = bvalue;
                    return Unsafe.As<bool, T>(ref v);
                }
            case LuaValueType.Float:
                {
                    var v = fvalue;
                    return Unsafe.As<double, T>(ref v);
                }
            case LuaValueType.Integer:
                {
                    var v = ivalue;
                    return Unsafe.As<long, T>(ref v);
                }
            case LuaValueType.String:
            case LuaValueType.Thread:
            case LuaValueType.Function:
            case LuaValueType.Table:
            case LuaValueType.LightUserData:
            case LuaValueType.UserData:
                {
                    var v = rvalue!;
                    return Unsafe.As<object, T>(ref v);
                }
        }

        return default!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ToBoolean()
    {
        if (type == LuaValueType.Boolean) return bvalue;
        if (type is LuaValueType.Nil) return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue(object obj)
    {
        type = LuaValueType.LightUserData;
        rvalue = obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue(LuaClosure value)
    {
        type = LuaValueType.Function;
        rvalue = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue(bool value)
    {
        type = LuaValueType.Boolean;
        bvalue = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue(double value)
    {
        type = LuaValueType.Float;
        fvalue = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue(long value)
    {
        type = LuaValueType.Integer;
        ivalue = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue(int value)
    {
        type = LuaValueType.Integer;
        ivalue = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue(string value)
    {
        type = LuaValueType.String;
        rvalue = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue(LuaCFunction value)
    {
        type = LuaValueType.CFunction;
        rvalue = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue(LuaTable value)
    {
        type = LuaValueType.Table;
        rvalue = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue(LuaState value)
    {
        type = LuaValueType.Thread;
        rvalue = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue(ILuaUserData value)
    {
        type = LuaValueType.UserData;
        rvalue = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LuaValue(bool value)
    {
        return new(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LuaValue(double value)
    {
        return new(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LuaValue(long value)
    {
        return new(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LuaValue(string value)
    {
        return new(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LuaValue(LuaTable value)
    {
        return new(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(bool value)
    {
        type = LuaValueType.Boolean;
        ivalue = value ? 1 : 0;
        rvalue = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(long value)
    {
        type = LuaValueType.Integer;
        ivalue = value;
        rvalue = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(double value)
    {
        type = LuaValueType.Float;
        fvalue = value;
        rvalue = null;
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static implicit operator LuaValue(LuaFunction value)
    //{
    //    return new(value);
    //}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LuaValue(LuaState value)
    {
        return new(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LuaValue(LuaCFunction value)
    {
        return new(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LuaValue(LuaClosure value)
    {
        return new(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return type switch
        {
            LuaValueType.Nil => 0,
            LuaValueType.Boolean => bvalue ? 1 : 0,
            LuaValueType.Float => fvalue.GetHashCode(),
            LuaValueType.Integer => ivalue.GetHashCode(),
            _ => rvalue?.GetHashCode() ?? 0
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(LuaValue other)
    {
        if (other.type != type) return false;

        return type switch
        {
            LuaValueType.Nil => true,
            LuaValueType.Boolean => bvalue == other.bvalue,
            LuaValueType.Float => fvalue == other.fvalue,
            LuaValueType.Integer => ivalue == other.ivalue,
            _ => rvalue?.Equals(other.rvalue) ?? false
        };
    }

    public override bool Equals(object? obj)
    {
        return obj is LuaValue value1 && Equals(value1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string UnsafeAsString() => Unsafe.As<string>(rvalue!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal double UnsafeAsDouble() => fvalue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal long UnsafeAsInteger() => ivalue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuaTable UnsafeAsTable() => Unsafe.As<LuaTable>(rvalue!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuaTable AsTable() => (LuaTable)(rvalue ?? throw new LuaException("LuaClosure cannot be null"));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuaClosure AsLuaClosure() => (LuaClosure)(rvalue ?? throw new LuaException("LuaClosure cannot be null"));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuaCFunction AsCFunction() => (LuaCFunction)(rvalue ?? throw new LuaException("LuaCFunction cannot be null"));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(LuaValue a, LuaValue b)
    {
        return a.Equals(b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(LuaValue a, LuaValue b)
    {
        return !a.Equals(b);
    }

    public override string ToString()
    {
        return type switch
        {
            LuaValueType.Nil => "nil",
            LuaValueType.Boolean => bvalue ? "true" : "false",
            LuaValueType.String => rvalue?.ToString() ?? "",
            LuaValueType.Float => fvalue.ToString(),
            LuaValueType.Integer => ivalue.ToString(),
            LuaValueType.Function => $"function: {rvalue?.GetHashCode()}",
            LuaValueType.CFunction => $"cfunction: {rvalue?.GetHashCode()}",
            LuaValueType.Thread => $"thread: {rvalue?.GetHashCode()}",
            LuaValueType.Table => $"table: {rvalue?.GetHashCode()}",
            LuaValueType.LightUserData => $"userdata: {rvalue?.GetHashCode()}",
            LuaValueType.UserData => $"userdata: {rvalue?.GetHashCode()}",
            _ => "",
        };
    }

    public static bool TryGetLuaValueType(Type type, out LuaValueType result)
    {
        if (type == typeof(double) || type == typeof(float) || type == typeof(int) || type == typeof(long))
        {
            result = LuaValueType.Float;
            return true;
        }
        else if (type == typeof(bool))
        {
            result = LuaValueType.Boolean;
            return true;
        }
        else if (type == typeof(string))
        {
            result = LuaValueType.String;
            return true;
        }
        //else if (type == typeof(LuaFunction) || type.IsSubclassOf(typeof(LuaFunction)))
        //{
        //    result = LuaValueType.Function;
        //    return true;
        //}
        else if (type == typeof(LuaTable))
        {
            result = LuaValueType.Table;
            return true;
        }
        else if (type == typeof(LuaState))
        {
            result = LuaValueType.Thread;
            return true;
        }
        else if (type == typeof(ILuaUserData) || type.IsAssignableFrom(typeof(ILuaUserData)))
        {
            result = LuaValueType.UserData;
            return true;
        }

        result = default;
        return false;
    }

    //internal ValueTask<int> CallToStringAsync(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    //{
    //    if (this.TryGetMetamethod(context.State, Metamethods.ToString, out var metamethod))
    //    {
    //        if (!metamethod.TryReadFunction(out var func))
    //        {
    //            LuaRuntimeException.AttemptInvalidOperation(context.State.GetTraceback(), "call", metamethod);
    //        }

    //        context.State.Push(this);

    //        return func.InvokeAsync(context with
    //        {
    //            ArgumentCount = 1,
    //            FrameBase = context.Thread.Stack.Count - 1,
    //        }, buffer, cancellationToken);
    //    }
    //    else
    //    {
    //        buffer.Span[0] = ToString();
    //        return new(1);
    //    }
    //}
}