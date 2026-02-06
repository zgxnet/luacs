using System.Runtime.InteropServices;
using BinaryWriter = Lua.Internal.BinaryWriter;
namespace Lua;

public static unsafe class ExternalCompiler
{
    // Delegate for the callback function that receives bytecode data
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WriteCallback(IntPtr data, int length, IntPtr userData);

    // P/Invoke declaration for the compile_lua function
    [DllImport("luac1.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void compile_lua(IntPtr source, WriteCallback func_write, IntPtr userData);

    static ExternalCompiler()
    {
        // The DLL will be loaded automatically when first P/Invoke call is made
        // No explicit loading needed with DllImport
    }

    public static byte[] Compile(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            throw new ArgumentException("Source code cannot be null or empty", nameof(source));
        }

        // Create buffer for collecting bytecode
        var bytecodeBuffer = new BinaryWriter();
        
        // Pin the buffer so we can pass it to native code
        GCHandle bufferHandle = GCHandle.Alloc(bytecodeBuffer);

        try
        {
            // Convert to UTF-8 bytes with null terminator
            int utf8Len = Encoding.UTF8.GetByteCount(source) + 1; // +1 for null terminator
            byte[] utf8Bytes = new byte[utf8Len];
            Encoding.UTF8.GetBytes(source, 0, source.Length, utf8Bytes, 0);

            // Use fixed statement to pin the UTF-8 bytes
            fixed (byte* utf8Ptr = utf8Bytes)
            {
                // Call the native function with our callback
                compile_lua((IntPtr)utf8Ptr, WriteCallbackImpl, GCHandle.ToIntPtr(bufferHandle));
            }

            // Return the collected bytecode
            return bytecodeBuffer.ToArray();
        }
        finally
        {
            // Clean up the handle
            bufferHandle.Free();
        }
    }

    // Callback implementation that receives bytecode chunks
    private static void WriteCallbackImpl(IntPtr data, int length, IntPtr userData)
    {
        if (data == IntPtr.Zero || length <= 0 || userData == IntPtr.Zero)
            return;

        // Get the buffer from the user data
        GCHandle bufferHandle = GCHandle.FromIntPtr(userData);
        var bytecodeBuffer = (BinaryWriter)bufferHandle.Target!;

        // Copy the data from unmanaged memory to our buffer
        Span<byte> chunk = new((byte*)data, length);

        bytecodeBuffer.WriteSpan(chunk);
    }
}
