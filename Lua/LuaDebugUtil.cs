namespace Lua;

public static class LuaDebugUtil
{
    public static List<string> GetStackTrace(LuaState luaState)
    {
        var stackTrace = new List<string>();
        var callStackFrames = luaState.GetCallStackFrames();
        var stack = luaState.stack;

        for (int i = 0; i < callStackFrames.Length; i++)
        {
            var frame = callStackFrames[i];
            if(frame.func < 0 || frame.func >= stack.Count)
            {
                stackTrace.Add($"Frame {i}: Invalid function index {frame.func}");
                continue;
            }
            var vfunc = stack[frame.func];
            if(!vfunc.IsLuaClosure)
            {
                stackTrace.Add($"Frame {i}: Function at index {frame.func} is not a Lua closure");
                continue;
            }
            var closure = stack[frame.func].AsLuaClosure();
            var proto = closure.proto;
            string frameInfo = $"Frame {i}: Function={proto.Name}, Line={proto.LineDefined}";
            stackTrace.Add(frameInfo);
        }

        return stackTrace;
    }
}
