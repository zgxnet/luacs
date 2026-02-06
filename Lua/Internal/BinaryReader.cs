namespace Lua.Internal;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;

public class BinaryReader
{
    byte[] _data;
    int _offset = 0;

    public BinaryReader(byte[] data)
    {
        _data = data;
    }

    public BinaryReader(string fname)
    {
        if (string.IsNullOrEmpty(fname))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fname));

        if (!File.Exists(fname))
            throw new System.IO.FileNotFoundException("File not found.", fname);

        _data = File.ReadAllBytes(fname);
    }

    T ReadT<T>() where T : unmanaged
    {
        int size = Unsafe.SizeOf<T>();
        EnsureSize(size);
        T value = Unsafe.ReadUnaligned<T>(ref _data[_offset]);
        _offset += size;
        return value;
    }

    public Span<byte> ReadSpan(int size)
    {
        EnsureSize(size);
        var span = _data.AsSpan(_offset, size);
        _offset += size;
        return span;
    }

    public ulong ReadUnsigned(ulong limit)
    {
        ulong x = 0;
        byte b;
        limit >>= 7;
        do
        {
            b = ReadByte();
            if (x >= limit)
                throw new InvalidOperationException("integer overflow");
            x = (x << 7) | ((uint)b & 0x7F);
        } while ((b & 0x80) == 0);
        return x;
    }

    public ulong ReadSize()
        => ReadUnsigned(Limits.MaxSizeT);

    public int ReadInt()
        => (int)(ReadUnsigned(int.MaxValue));

    public double ReadNumber()
        => ReadT<double>();

    public long ReadInteger()
        => ReadT<long>();

    public void ReadBuffer<T>(T[] buffer) where T : unmanaged
    {
        if (buffer.Length == 0) return;
        var sz = Unsafe.SizeOf<T>() * buffer.Length;
        EnsureSize(sz);
        var span = MemoryMarshal.Cast<byte, T>(_data.AsSpan(_offset, sz));
        span.CopyTo(buffer);
        _offset += sz;
    }

    public string? ReadString()
    {
        ulong size = ReadSize();
        if (size == 0)
            return null;
        size--;
        if (size > int.MaxValue)
            throw new InvalidOperationException("String size exceeds maximum allowed size.");
        Span<byte> bytes = ReadSpan((int)size);
        return Encoding.UTF8.GetString(bytes);
    }

    public string ReadStringNotNull()
        => ReadString() ?? throw new InvalidOperationException("bad format for constant string");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool()
    {
        byte b= ReadByte();
        if (b == 0) return false;
        else if (b == 1) return true;
        else
            throw new InvalidOperationException($"expect 0/1 for boolean, got {b}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        EnsureSize(1);
        return _data[_offset++];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void EnsureSize(int size)
    {
        if (_offset + size > _data.Length)
            throw new EndOfStreamException("Not enough data to read the requested size.");
    }
}
