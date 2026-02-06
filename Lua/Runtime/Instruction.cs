using System.Runtime.CompilerServices;

namespace Lua.Runtime;

public struct Instruction
{
    public uint Value;
    const int SIZE_A = 8;
    const int SIZE_B = 8;
    const int SIZE_C = 8;

    internal const int MAXARG_A = ((1 << SIZE_A) - 1);
    internal const int MAXARG_B = ((1 << SIZE_B) - 1);
    internal const int MAXARG_C = ((1 << SIZE_C) - 1);
    const int OFFSET_sC = (MAXARG_C >> 1);

    public OpCode OpCode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (OpCode)(byte)(Value & 0x7F); // 7 bits
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = (Value & 0xFFFFFF80) | ((uint)value & 0x7F);
    }

    public byte A
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte)((Value >> 7)); // 8 bits
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = (Value & 0xFFFF807F) | (((uint)value & 0xFF) << 7);
    }

    public byte B
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte)((Value >> 16) & 0xFF); // 8 bits
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = (Value & 0xFF00FFFF) | (((uint)value & 0xFF) << 16);
    }

    public int SB => B - OFFSET_sC;

    public int SC => C - OFFSET_sC;

    public bool K
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ((Value >> 15) & 1) > 0; // 1 bit
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = (Value & 0xFFFF7FFF) | ((value ? 1u : 0) << 15);
    }

    public byte C
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte)((Value >> 24) & 0xFF); // 8 bits
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = (Value & 0x00FFFFFF) | (((uint)value & 0xFF) << 24);
    }

    public int Bx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)((Value >> 15) & 0x1FFFF); // 17 bits
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = (Value & 0x00007FFF) | (((uint)value & 0x1FFFF) << 15);
    }

    const int MAXARG_Bx = (1 << 17) - 1;
    const int OFFSET_sBx = MAXARG_Bx >> 1;

    public int SBx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)(Bx - OFFSET_sBx); // signed 17 bits
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Bx = (value + OFFSET_sBx);
    }

    public int Ax
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)(Value >> 7) & 0x3FFFFFF; // 25 bits
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = (Value & 0x0000007F) | (((uint)value & 0x1FFFFFF) << 7);
    }

    const int MAXARG_J = (1 << 25) - 1;
    const int OFFSET_sJ = (MAXARG_J >> 1);
    public int SJ
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Ax - OFFSET_sJ; // signed 25 bits
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Ax = value + OFFSET_sJ;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Instruction other)
    {
        return Value == other.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (obj is Instruction instruction) return Equals(instruction);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return OpCode switch
        {
            OpCode.Move => $"MOVE      {A} {B}",
            OpCode.LoadI => $"LOADI     {A} {SBx}",
            OpCode.LoadF => $"LOADF     {A} {SBx}",
            OpCode.LoadK => $"LOADK     {A} {Bx}",
            OpCode.LoadKx => $"LOADKX    {A}",
            OpCode.LoadFalse => $"LOADFALSE {A}",
            OpCode.LFalseSkip => $"LFALSESKIP {A}",
            OpCode.LoadTrue => $"LOADTRUE  {A}",
            OpCode.LoadNil => $"LOADNIL   {A} {B}",
            OpCode.GetUpVal => $"GETUPVAL  {A} {B}",
            OpCode.SetUpVal => $"SETUPVAL  {A} {B}",
            OpCode.GetTabUp => $"GETTABUP  {A} {B} {C}",
            OpCode.GetTable => $"GETTABLE  {A} {B} {C}",
            OpCode.GetI => $"GETI      {A} {B} {C}",
            OpCode.GetField => $"GETFIELD  {A} {B} {C}",
            OpCode.SetTabUp => $"SETTABUP  {A} {B} {C}",
            OpCode.SetTable => $"SETTABLE  {A} {B} {C}",
            OpCode.SetI => $"SETI      {A} {B} {C}",
            OpCode.SetField => $"SETFIELD  {A} {B} {C}",
            OpCode.NewTable => $"NEWTABLE  {A} {B} {C} {K}",
            OpCode.Self => $"SELF      {A} {B} {C}",
            OpCode.AddI => $"ADDI      {A} {B} {SBx}",
            OpCode.AddK => $"ADDK      {A} {B} {C}",
            OpCode.SubK => $"SUBK      {A} {B} {C}",
            OpCode.MulK => $"MULK      {A} {B} {C}",
            OpCode.ModK => $"MODK      {A} {B} {C}",
            OpCode.PowK => $"POWK      {A} {B} {C}",
            OpCode.DivK => $"DIVK      {A} {B} {C}",
            OpCode.IDivK => $"IDIVK     {A} {B} {C}",
            OpCode.BandK => $"BANDK     {A} {B} {C}",
            OpCode.BorK => $"BORK      {A} {B} {C}",
            OpCode.BxorK => $"BXORK     {A} {B} {C}",
            OpCode.ShrI => $"SHRI      {A} {B} {SBx}",
            OpCode.ShlI => $"SHLI      {A} {B} {SBx}",
            OpCode.Add => $"ADD       {A} {B} {C}",
            OpCode.Sub => $"SUB       {A} {B} {C}",
            OpCode.Mul => $"MUL       {A} {B} {C}",
            OpCode.Mod => $"MOD       {A} {B} {C}",
            OpCode.Pow => $"POW       {A} {B} {C}",
            OpCode.Div => $"DIV       {A} {B} {C}",
            OpCode.IDiv => $"IDIV      {A} {B} {C}",
            OpCode.Band => $"BAND      {A} {B} {C}",
            OpCode.Bor => $"BOR       {A} {B} {C}",
            OpCode.Bxor => $"BXOR      {A} {B} {C}",
            OpCode.Shl => $"SHL       {A} {B} {C}",
            OpCode.Shr => $"SHR       {A} {B} {C}",
            OpCode.MmBin => $"MMBIN     {A} {B} {C}",
            OpCode.MmBinI => $"MMBINI    {A} {SB} {C} {K}",
            OpCode.MmBinK => $"MMBINK    {A} {B} {C} {K}",
            OpCode.Unm => $"UNM       {A} {B}",
            OpCode.BNot => $"BNOT      {A} {B}",
            OpCode.Not => $"NOT       {A} {B}",
            OpCode.Len => $"LEN       {A} {B}",
            OpCode.Concat => $"CONCAT    {A} {B}",
            OpCode.Close => $"CLOSE     {A}",
            OpCode.Tbc => $"TBC       {A}",
            OpCode.Jmp => $"JMP       {SJ}",
            OpCode.Eq => $"EQ        {A} {B} {K}",
            OpCode.Lt => $"LT        {A} {B} {K}",
            OpCode.Le => $"LE        {A} {B} {K}",
            OpCode.EqK => $"EQK       {A} {B} {K}",
            OpCode.EqI => $"EQI       {A} {SBx} {K}",
            OpCode.LtI => $"LTI       {A} {SBx} {K}",
            OpCode.LeI => $"LEI       {A} {SBx} {K}",
            OpCode.GtI => $"GTI       {A} {SBx} {K}",
            OpCode.GeI => $"GEI       {A} {SBx} {K}",
            OpCode.Test => $"TEST      {A} {K}",
            OpCode.TestSet => $"TESTSET   {A} {B} {K}",
            OpCode.Call => $"CALL      {A} {B} {C}",
            OpCode.TailCall => $"TAILCALL  {A} {B} {C} {K}",
            OpCode.Return => $"RETURN    {A} {B} {C} {K}",
            OpCode.Return0 => $"RETURN0",
            OpCode.Return1 => $"RETURN1   {A}",
            OpCode.ForLoop => $"FORLOOP   {A} {Bx}",
            OpCode.ForPrep => $"FORPREP   {A} {Bx}",
            OpCode.TForPrep => $"TFORPREP  {A} {Bx}",
            OpCode.TForCall => $"TFORCALL  {A} {C}",
            OpCode.TForLoop => $"TFORLOOP  {A} {Bx}",
            OpCode.SetList => $"SETLIST   {A} {B} {C} {K}",
            OpCode.Closure => $"CLOSURE   {A} {Bx}",
            OpCode.VarArg => $"VARARG    {A} {C}",
            OpCode.VarArgPrep => $"VARARGPREP {A}",
            OpCode.ExtraArg => $"EXTRAARG  {Ax}",
            _ => $"UNKNOWN    {Value}",
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Instruction left, Instruction right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Instruction left, Instruction right)
    {
        return !(left == right);
    }
}
