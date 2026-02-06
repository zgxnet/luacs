namespace Lua.Runtime;

public class Proto
{
    public string? Name;           //zgx: no name in standard lua bytecode, but we can add it for debugging purposes
    public byte NumParams;         // number of fixed (named) parameters
    public bool IsVarArg;
    public byte MaxStackSize;      // number of registers needed by this function

    public int LineDefined;        // debug information
    public int LastLineDefined;    // debug information

    public LuaValue[] K = [];           // constants used by the function
    public Instruction[] Code = [];     // opcodes
    public Proto[] P = [];              // functions defined inside the function
    public UpValueDesc[] UpValues = []; // upvalue information
    public sbyte[]? LineInfo;            // information about source lines (debug information), see luaG_getfuncline
    public AbsLineInfo[]? AbsLineInfo;  // idem
    public LocVar[] LocVars = [];       // information about local variables (debug information)
    public string? Source;              // used for debug information

    internal int InternalId; //internal Id, used for debugging purposes
}
