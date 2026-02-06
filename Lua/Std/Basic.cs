using System.ComponentModel;

namespace Lua.Std;

internal class Basic
{
    static int IOWrite(LuaState L, bool writeLine)
    {
        int nparams = L.GetTop();
        StringBuilder sb = new();
        for (int i = 1; i <= nparams; i++)
        {
            var v = L.GetValue(i);
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append(v.ToString());
        }
        if (writeLine)
            Console.WriteLine(sb);
        else
            Console.Write(sb);
        return 0;
    }
    
    public static int Print(LuaState L)
        => IOWrite(L, true);

    public static int Write(LuaState L)
    {
        IOWrite(L, false);
        Console.Out.Flush();
        return 0;
    }

    public static int NewTable(LuaState L)
    {
        int nparams = L.GetTop();
        if (nparams == 0)
        {
            L.PushValue(new LuaTable());
        }
        else if (nparams == 1)
        {
            if (L.GetValue(1).TryToInteger(out var asize) && (ulong)asize < int.MaxValue)
                L.PushValue(new LuaTable((int)asize, 0));
            else
                throw new LuaException($"Invalid parameter for table array size, must be a positive integer (<{int.MaxValue})");
        }
        else if (nparams == 2)
        {
            if (L.GetValue(1).TryToInteger(out var asize) && (ulong)asize < int.MaxValue &&
                L.GetValue(2).TryToInteger(out var dsize) && (ulong)dsize < int.MaxValue)
            {
                L.PushValue(new LuaTable((int)asize, (int)dsize));
            }
            else
            {
                throw new LuaException($"Invalid parameters for table array / dictionary size, must be positive integers (<{int.MaxValue})");
            }
        }
        else
        {
            throw new LuaException("Invalid number of parameters for table creation");
        }
        return 1;
    }

    static class Runtime
    {
        static Runtime()
        {
            var ThisProcess = System.Diagnostics.Process.GetCurrentProcess(); LastSystemTime = (long)(System.DateTime.Now - ThisProcess.StartTime).TotalMilliseconds; ThisProcess.Dispose();
            StopWatch = new System.Diagnostics.Stopwatch(); StopWatch.Start();
        }
        private static long LastSystemTime;
        private static System.Diagnostics.Stopwatch StopWatch;

        public static long CurrentRuntime { get { return StopWatch.ElapsedMilliseconds + LastSystemTime; } }
    }

    public static int Clock(LuaState L)
    {
        L.PushValue(Runtime.CurrentRuntime / 1000.0);
        return 1;
    }

    class RandomGenerator
    {
        private Random? _random = null;

        Random Instance => _random ??= new Random();

        public int Random(LuaState L)
        {
            int nparams = L.GetTop();
            if (nparams == 0)
            {
                L.PushValue(Instance.NextDouble());
            }
            else if (nparams == 1)
            {
                var v = L.GetValue(1);
                if (v.TryToInteger(out long i) && i >= 0 && i != long.MaxValue)
                {
                    if (i == 0)
                        L.PushValue(Instance.NextInt64());
                    else
                        L.PushValue(Instance.NextInt64(i + 1));
                }
                else
                    throw new Exception($"Invalid parameter {v}");
            }
            else if (nparams == 2)
            {
                var v1 = L.GetValue(1);
                var v2 = L.GetValue(2);
                if (v1.TryToInteger(out long i) && i >= 0 && i != long.MaxValue &&
                    v2.TryToInteger(out long j) && j >= 0 && j != long.MaxValue &&
                    i < j)
                {
                    L.PushValue(Instance.NextInt64(i, j + 1));
                }
            }
            return 1;
        }

    }


    public static void Register(LuaTable env)
    {
        env["print"] = new LuaCFunction(Print);
        env["math"] = CreateMath();
        env["os"] = CreateOS();
        env["io"] = CreateIO();
        env["table"] = CreateTable();
    }

    static LuaTable CreateMath()
    {
        LuaTable math = new LuaTable();
        var generator = new RandomGenerator();
        math["random"] = new LuaCFunction(generator.Random);
        return math;
    }

    static LuaTable CreateOS()
    {
        LuaTable os = new LuaTable();
        os["clock"] = new LuaCFunction(Clock);
        return os;
    }

    static LuaTable CreateIO()
    {
        LuaTable io = new LuaTable();
        io["write"] = new LuaCFunction(Write);
        return io;
    }

    static LuaTable CreateTable()
    {
        LuaTable table = new LuaTable();
        table["new"] = new LuaCFunction(NewTable);
        return table;
    }
}
