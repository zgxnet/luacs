namespace Lua.Internal;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

public class BinaryWriter
{
    private byte[] _buffer;
    private int _position;
    private bool _disposed = false;

    public BinaryWriter(int initialCapacity = 256)
    {
        _buffer = new byte[initialCapacity];
        _position = 0;
    }

    public void WriteToFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        File.WriteAllBytes(fileName, ToArray());
    }

    public byte[] ToArray()
    {
        return _buffer.AsSpan(0, _position).ToArray();
    }

    public int Length => _position;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void EnsureCapacity(int additionalBytes)
    {
        int requiredCapacity = _position + additionalBytes;
        if (requiredCapacity > _buffer.Length)
        {
            int newCapacity = Math.Max(_buffer.Length * 2, requiredCapacity);
            Array.Resize(ref _buffer, newCapacity);
        }
    }

    void WriteT<T>(T value) where T : unmanaged
    {
        int size = Unsafe.SizeOf<T>();
        EnsureCapacity(size);
        Unsafe.WriteUnaligned(ref _buffer[_position], value);
        _position += size;
    }

    public void WriteSpan(ReadOnlySpan<byte> span)
    {
        EnsureCapacity(span.Length);
        span.CopyTo(_buffer.AsSpan(_position));
        _position += span.Length;
    }

    public void WriteUnsigned(ulong value)
    {
        List<byte> bytes = new List<byte>();
        
        // Handle the case where value is 0
        if (value == 0)
        {
            WriteByte(0x80); // 0 with continuation bit set
            return;
        }

        // Extract 7-bit chunks from the value
        while (value > 0)
        {
            byte b = (byte)(value & 0x7F);
            value >>= 7;
            bytes.Add(b);
        }

        // Write bytes in reverse order (most significant first)
        // Set continuation bit (0x80) for all bytes except the last one
        for (int i = bytes.Count - 1; i >= 0; i--)
        {
            byte b = bytes[i];
            if (i > 0)
                b |= 0x80; // Set continuation bit
            else
                b &= 0x7F; // Clear continuation bit for last byte
            WriteByte(b);
        }
    }

    public void WriteSize(ulong size)
    {
        if (size > Limits.MaxSizeT)
            throw new ArgumentOutOfRangeException(nameof(size), "Size exceeds maximum allowed value.");
        WriteUnsigned(size);
    }

    public void WriteInt(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative.");
        WriteUnsigned((ulong)value);
    }

    public void WriteNumber(double value)
        => WriteT(value);

    public void WriteInteger(long value)
        => WriteT(value);

    public void WriteBuffer<T>(T[] buffer) where T : unmanaged
    {
        if (buffer.Length == 0) return;
        
        var sz = Unsafe.SizeOf<T>() * buffer.Length;
        EnsureCapacity(sz);
        var span = MemoryMarshal.Cast<T, byte>(buffer.AsSpan());
        span.CopyTo(_buffer.AsSpan(_position));
        _position += sz;
    }

    public void WriteString(string? value)
    {
        if (value == null)
        {
            WriteSize(0);
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value);
        WriteSize((ulong)(bytes.Length + 1)); // +1 for null terminator in Lua format
        WriteSpan(bytes);
    }

    public void WriteStringNotNull(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "String cannot be null.");
        WriteString(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value)
    {
        WriteByte(value ? (byte)1 : (byte)0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        EnsureCapacity(1);
        _buffer[_position++] = value;
    }
}
