using System;
using System.Runtime.CompilerServices;

public static class Limits
{
    // Maximum value for size_t
    public const ulong MaxSizeT = ulong.MaxValue;

    // Maximum size visible for Lua (假设 lua_Integer 为 long)
    public const long MaxSize = long.MaxValue;

    // Maximum value for lu_mem (假设 lu_mem 为 ulong)
    public const ulong MaxLumem = ulong.MaxValue;

    // Maximum value for l_mem (假设 l_mem 为 long)
    public const long MaxLmem = long.MaxValue;

    // Maximum value of an int
    public const int MaxInt = int.MaxValue;

    // Floor of the log2 of the maximum signed value for integral type 'T'
    public static int Log2MaxS<T>() where T : unmanaged
        => Unsafe.SizeOf<T>() * 8 - 2;

    // Test whether an unsigned value is a power of 2 (or zero)
    public static bool IsPow2(ulong x) => (x & (x - 1)) == 0;

    // Number of chars of a literal string without the ending \0
    public static int Ll(string x) => x.Length;

    // Maximum length for short strings
    public const int MaxShortLen = 40;

    // Initial size for the string table (must be power of 2)
    public const int MinStrTabSize = 128;

    // Size of cache for strings in the API
    public const int StrCacheN = 53;
    public const int StrCacheM = 2;

    // Minimum size for string buffer
    public const int LuaMinBuffer = 32;

    // Maximum depth for nested C calls, etc.
    public const int LuaiMaxCCalls = 200;
}
