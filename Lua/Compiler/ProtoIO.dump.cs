using Lua.Runtime;

namespace Lua.Compiler;

partial class ProtoIO
{
    public static void DumpProto(string fname, Proto proto)
    {
        using var writer = new StreamWriter(fname);
        void Dump(Proto p)
        {
            writer.WriteLine($"Proto: {p.InternalId} {(p.Name ?? "<unnamed>")}");
            writer.WriteLine($"  NumParams: {p.NumParams}");
            writer.WriteLine($"  IsVarArg: {p.IsVarArg}");
            writer.WriteLine($"  MaxStackSize: {p.MaxStackSize}");
            writer.WriteLine($"  LineDefined: {p.LineDefined}");
            writer.WriteLine($"  LastLineDefined: {p.LastLineDefined}");
            writer.WriteLine($"  Source: {p.Source ?? "<none>"}");

            writer.WriteLine($"  Code ({p.Code.Length}):");
            for (int i = 0; i < p.Code.Length; i++)
            {
                writer.WriteLine($"    [{i}]: {p.Code[i]}");
            }

            writer.WriteLine($"  Constants ({p.K.Length}):");
            for (int i = 0; i < p.K.Length; i++)
            {
                writer.WriteLine($"    [{i}]: {p.K[i]}");
            }

            writer.WriteLine($"  UpValues ({p.UpValues.Length}):");
            for (int i = 0; i < p.UpValues.Length; i++)
            {
                var uv = p.UpValues[i];
                writer.WriteLine($"    [{i}]: Name={uv.Name}, InStack={uv.InStack}, Index={uv.Index}, Kind={uv.Kind}");
            }

            writer.WriteLine($"  LocVars ({p.LocVars.Length}):");
            for (int i = 0; i < p.LocVars.Length; i++)
            {
                var lv = p.LocVars[i];
                writer.WriteLine($"    [{i}]: Name={lv.VarName}, StartPC={lv.StartPC}, EndPC={lv.EndPC}");
            }

            //if (p.LineInfo != null)
            //{
            //    writer.WriteLine($"  LineInfo ({p.LineInfo.Length}): {string.Join(", ", p.LineInfo)}");
            //}
            //if (p.AbsLineInfo != null)
            //{
            //    writer.WriteLine($"  AbsLineInfo ({p.AbsLineInfo.Length}):");
            //    for (int i = 0; i < p.AbsLineInfo.Length; i++)
            //    {
            //        var ali = p.AbsLineInfo[i];
            //        writer.WriteLine($"    [{i}]: PC={ali.PC}, Line={ali.Line}");
            //    }
            //}

            writer.WriteLine($"  SubProtos ({p.P.Length}): {string.Join(" ", p.P.Select(_ => _.InternalId))}");
            writer.WriteLine();
            writer.WriteLine("---------------------------------------------------------------");
            writer.WriteLine();

            for (int i = 0; i < p.P.Length; i++)
                Dump(p.P[i]);
        }
        Dump(proto);
    }
}
