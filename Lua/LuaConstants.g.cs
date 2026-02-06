namespace Lua;
partial class LuaConstants
{
    public const int TNil = 0;
    public const int TBoolean = 1;
    public const int TLightUserData = 2;
    public const int TNumber = 3;
    public const int TString = 4;
    public const int TTable = 5;
    public const int TFunction = 6;
    public const int TUserData = 7;
    public const int TThread = 8;
    public const int NumTypes = 9;

    /* Standard nil */
    public const int VNIL = 0;

    /* Booleans*/
    public const int VFALSE = 1;
    public const int VTRUE = 17;

    /* Threads */
    public const int VTHREAD = 8;

    /* Variant tags for numbers */
    public const int VNUMINT = 3; /* integer numbers */
    public const int VNUMFLT = 19; /* float numbers */

    /* Variant tags for strings */
    public const int VSHRSTR = 4; /* short strings */
    public const int VLNGSTR = 20; /* long strings */

    /* Tables*/
    public const int VTABLE = 5; 
}

