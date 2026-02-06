using Lua;
using Lua.Internal;
using Lua.Runtime;
using System.Diagnostics;
using System.Xml;
using LuaBinaryReader = Lua.Internal.BinaryReader;
namespace Lua.Compiler;

public static partial class ProtoIO
{
    /* mark for precompiled code ('<esc>Lua') */
    static byte[] SIGNATURE = { 0x1B, (byte)'L', (byte)'u', (byte)'a' };
    static byte[] LUAC_DATA = { 0x19, 0x93, 0x0D, 0x0A, 0x1A, 0x0A };

    class Loader
    {
        readonly LuaBinaryReader reader;
        int id;

        public Loader(LuaBinaryReader reader)
        {
            this.reader = reader;
        }

        void Checkliteral(byte[] s, string msg)
        {
            Span<byte> bytes = reader.ReadSpan(s.Length);
            if (!bytes.SequenceEqual(s))
                throw new LuaException(msg);
        }

        void CheckHeader()
        {
            // Skip 1st char (already read and checked before calling this)
            Checkliteral(SIGNATURE, "not a binary chunk");

            // Version check
            byte version = reader.ReadByte();
            if (version != LuaConstants.LuacVersion)
                throw new LuaException("version mismatch");

            // Format check
            byte format = reader.ReadByte();
            if (format != LuaConstants.LuacFormat)
                throw new LuaException("format mismatch");

            // LUAC_DATA check
            Checkliteral(LUAC_DATA, "corrupted chunk");

            // Size checks
            if (reader.ReadByte() != sizeof(uint)) // Instruction size
                throw new LuaException("Instruction size mismatch");
            if (reader.ReadByte() != sizeof(long)) // lua_Integer size
                throw new LuaException("lua_Integer size mismatch");
            if (reader.ReadByte() != sizeof(double)) // lua_Number size
                throw new LuaException("lua_Number size mismatch");

            // Integer format check
            if (reader.ReadInteger() != LuaConstants.LuacInt)
                throw new LuaException("integer format mismatch");

            // Number format check
            if (reader.ReadNumber() != LuaConstants.LuacNum)
                throw new LuaException("float format mismatch");
        }

        void LoadCode(Proto f)
        {
            int n = reader.ReadInt();
            f.Code = new Instruction[n];
            reader.ReadBuffer(f.Code);
        }

        void LoadConstants(Proto f)
        {
            int n = reader.ReadInt();
            f.K = new LuaValue[n];
            for (int i = 0; i < n; i++)
            {
                int t = reader.ReadByte();
                switch (t)
                {
                    case LuaConstants.VNIL:
                        f.K[i] = LuaValue.Nil;
                        break;
                    case LuaConstants.VFALSE:
                        f.K[i] = new LuaValue(false);
                        break;
                    case LuaConstants.VTRUE:
                        f.K[i] = new LuaValue(true);
                        break;
                    case LuaConstants.VNUMFLT:
                        f.K[i] = new LuaValue(reader.ReadNumber());
                        break;
                    case LuaConstants.VNUMINT:
                        f.K[i] = new LuaValue((double)reader.ReadInteger());
                        break;
                    case LuaConstants.VSHRSTR:
                    case LuaConstants.VLNGSTR:
                        f.K[i] = new LuaValue(reader.ReadStringNotNull());
                        break;
                    default:
                        throw new LuaException($"unknown constant type: {t}");
                }
            }
        }

        void LoadUpvalues(Proto f)
        {
            int n = reader.ReadInt();
            f.UpValues = new UpValueDesc[n];
            for (int i = 0; i < n; i++)
            {
                f.UpValues[i] = new UpValueDesc
                {
                    InStack = reader.ReadBool(),
                    Index = reader.ReadByte(),
                    Kind = reader.ReadByte()
                };
            }
        }

        void LoadProtos(Proto f)
        {
            int n = reader.ReadInt();
            f.P = new Proto[n];
            for (int i = 0; i < n; i++)
            {
                Proto f1;
                f.P[i] = f1 = new Proto();
                f1.InternalId = id++;
                LoadFunction(f.P[i]);
            }
        }

        void LoadDebug(Proto f)
        {
            int n = reader.ReadInt();
            f.LineInfo = new sbyte[n];
            reader.ReadBuffer(f.LineInfo);
            n = reader.ReadInt();
            f.AbsLineInfo = new AbsLineInfo[n];
            for (int i = 0; i < n; i++)
            {
                f.AbsLineInfo[i] = new AbsLineInfo
                {
                    PC = reader.ReadInt(),
                    Line = reader.ReadInt()
                };
            }
            //local vars
            n = reader.ReadInt();
            f.LocVars = new LocVar[n];
            for (int i = 0; i < n; i++)
            {
                f.LocVars[i] = new LocVar
                {
                    VarName = reader.ReadStringNotNull(),
                    StartPC = reader.ReadInt(),
                    EndPC = reader.ReadInt()
                };
            }
            //upvalues
            n = reader.ReadInt();
            if (n != 0)
                n = f.UpValues.Length;
            for (int i = 0; i < n; i++)
                f.UpValues[i].Name = reader.ReadStringNotNull();
        }

        void LoadFunction(Proto f)
        {
            f.Source = reader.ReadString();
            f.LineDefined = reader.ReadInt();
            f.LastLineDefined = reader.ReadInt();
            f.NumParams = reader.ReadByte();
            f.IsVarArg = reader.ReadBool();
            f.MaxStackSize = reader.ReadByte();
            LoadCode(f);
            LoadConstants(f);
            LoadUpvalues(f);
            LoadProtos(f);
            LoadDebug(f);
            // Further processing for nested functions, upvalues, etc.
        }

        public Proto LoadProto()
        {
            CheckHeader();
            byte nupvals = reader.ReadByte();
            Proto f = new Proto();
            f.InternalId = id++;
            LoadFunction(f);
            return f;
        }
    }

    static Proto LoadProto(LuaBinaryReader reader)
    {
        var f = new Loader(reader).LoadProto();
        GuessSubProtoNamesRec(f);
        return f;
    }

    static void GuessSubProtoNames(Proto f)
    {
        for(int i = 0; i < f.Code.Length - 1; i++)
        {
            Instruction iClosure = f.Code[i];
            if (iClosure.OpCode != OpCode.Closure)
                continue;
            int r = iClosure.A;
            int protoIndex = iClosure.Bx;
            if (protoIndex >= f.P.Length)
                continue;
            if(r < f.LocVars.Length) //local
            {
                f.P[protoIndex].Name = f.LocVars[r].VarName;
            }
            else //non-local
            {
                Instruction iSetup = f.Code[i + 1];
                if (iSetup.OpCode == OpCode.SetTabUp)
                {
                    var b = iSetup.B;
                    if (b >= f.K.Length)
                        continue;
                    if (iSetup.K || iSetup.C != r) continue;
                    LuaValue luaValue = f.K[b];
                    if (luaValue.TryReadString(out string? name))
                    {
                        f.P[protoIndex].Name = name;
                    }
                }
            }

        }
    }

    static void GuessSubProtoNamesRec(Proto f)
    {
        GuessSubProtoNames(f);
        foreach (var f1 in f.P)
            GuessSubProtoNames(f1);
    }

    public static Proto LoadProto(string fname)
    {
        var reader = new LuaBinaryReader(fname);
        return LoadProto(reader);
    }

    public static Proto LoadProto(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty.", nameof(data));
        var reader = new LuaBinaryReader(data);
        return LoadProto(reader);
    }
}
