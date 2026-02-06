namespace Lua;

public enum LuaValueType
{
    Nil = 0,
    Boolean,
    String,
    Float,
    Integer,
    Function,
    CFunction,
    Thread,
    LightUserData,
    UserData,
    Table,
}
