using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lua.Internal;

[StructLayout(LayoutKind.Auto)]
public struct FastStackCore<T> where T :struct
{
    const int InitialCapacity = 8;

    public FastStackCore()
    {}

    internal T[] array = [];
    internal int top;

    public int Count => top; //to remove
    public int Top => top;

    public readonly ReadOnlySpan<T> AsSpan() => array.AsSpan(0, top);

    public readonly Span<T> GetBuffer() => array.AsSpan();

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {return ref array[index];}
    }

    public ref T GetFirstRef()
        => ref MemoryMarshal.GetArrayDataReference(array);

    public ref T GetTopRef()
    {
        if (top == 0)
            ThrowForEmptyStack();
        return ref array[top - 1]!;
    }

    public void Push(scoped in T item)
    {
        CheckCapacity(top + 1);
        array[top] = item;
        top++;
    }

    public ref T PushAndRef()
    {
        CheckCapacity(top + 1);
        ref var v = ref array[top];
        top++;
        return ref v;
    }

    public ref T APushAndRef(out bool realloced)
    {
        realloced = EnsureCapacity(top + 1);
        ref var v = ref array[top];
        top++;
        return ref v;
    }

    public void APush(scoped in T item)
    {
        EnsureCapacity(top + 1);
        array[top++] = item;
    }

    public void APush(scoped in T item, out bool realloced)
    {
        realloced = EnsureCapacity(top + 1);
        array[top++] = item;
    }

    public bool TryPop(out T value)
    {
        if (top == 0)
        {
            value = default;
            return false;
        }

        top--;
        value = array[top]!;
        array[top] = default;

        return true;
    }

    internal bool TryPop()
    {
        if (top == 0)
            return false;
        array[--top] = default;
        return true;
    }

    public T Pop()
    {
        if (!TryPop(out var result)) ThrowForEmptyStack();
        return result;
    }

    public void PopUntil(int newSize)
    {
        if (newSize >= top) return;
        array.AsSpan(newSize, top - newSize).Clear();
        top = newSize;
    }

    public void SetSize(int newSize)
    {
        Debug.Assert(newSize >= 0);
        if (newSize == top) return;
        if(newSize < top)
            array.AsSpan(newSize, top - newSize).Clear();
        else //newSize > top
        {
            EnsureCapacity(newSize);
            //nothing todo
        }
        top = newSize;
    }

    public bool TryPeek(out T value)
    {
        if (top == 0)
        {
            value = default!;
            return false;
        }

        value = array[top - 1]!;
        return true;
    }

    public T Peek()
    {
        if (!TryPeek(out var result)) ThrowForEmptyStack();
        return result;
    }

    public void ReserveMore(int capacity, out bool realloced)
    {
        Debug.Assert(capacity >= 0);
        realloced = EnsureCapacity(top + capacity);
    }

    public void ReserveMore(int capacity)
    {
        Debug.Assert(capacity >= 0);
        EnsureCapacity(top + capacity);
    }

    internal ref T PeekRef()
    {
        if (top == 0)
            ThrowForEmptyStack();

        return ref array[top - 1]!;
    }

    internal ref T PeekRefOrNull()
    {
        if (top == 0)
            return ref Unsafe.NullRef<T>();
        return ref array[top - 1]!;
    }

    void CheckCapacity(int capacity)
    {
        if ((uint)capacity > array.Length)
            throw new InvalidOperationException("Insufficient stack capacity.");
    }

    public bool EnsureCapacity(int capacity)
    {
        var newSize = array.Length;
        if (newSize >= capacity)
            return false;
        else if (newSize == 0)
            newSize = InitialCapacity;
        while (newSize < capacity)
                newSize *= 2;
        Array.Resize(ref array, newSize);
        return true;
    }

    public void NotifyTop(int top)
    {
        if (this.top < top) this.top = top;
    }

    public void Clear()
    {
        array.AsSpan(0, top).Clear();
        top = 0;
    }

    [DoesNotReturn]
    void ThrowForEmptyStack()
        => throw new InvalidOperationException("Empty stack");
}
