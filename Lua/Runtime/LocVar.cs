namespace Lua.Runtime;

//see lobject.h: LocVar

/*
** Description of a local variable for function prototypes
** (used for debug information)
*/
public class LocVar
{
    public string VarName = "";
    public int StartPC;  /* first point where variable is active */
    public int EndPC;    /* first point where variable is dead */
}
