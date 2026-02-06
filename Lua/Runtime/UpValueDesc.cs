namespace Lua.Runtime;

//see Upvaldesc
public class UpValueDesc
{
    /// <summary>
    /// Upvalue name (for debug information)
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Whether it is in stack (register)
    /// </summary>
    public bool InStack;

    /// <summary>
    /// Index of upvalue (in stack or in outer function's list)
    /// </summary>
    public byte Index;

    /// <summary>
    /// Kind of corresponding variable
    /// </summary>
    public byte Kind;
}
