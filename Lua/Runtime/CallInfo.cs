namespace Lua.Runtime;

//see C: CallInfo
internal struct CallInfo
{
    public int func; //function index in the stack, func+1 is the stack base
    public int top; //top for this function
    public int savedpc; //for Lua Function

    public int nresults;  //expected number of results from this function
    public int nextraargs;  // # of extra arguments in vararg functions
}
