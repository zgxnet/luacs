namespace Lua.Runtime;
/*===========================================================================
  We assume that instructions are unsigned 32-bit integers.
  All instructions have an opcode in the first 7 bits.
  Instructions can have the following formats:

        3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1 0 0 0 0 0 0 0 0 0 0
        1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
iABC          C(8)     |      B(8)     |k|     A(8)      |   Op(7)     |
iABx                Bx(17)               |     A(8)      |   Op(7)     |
iAsBx              sBx (signed)(17)      |     A(8)      |   Op(7)     |
iAx                           Ax(25)                     |   Op(7)     |
isJ                           sJ (signed)(25)            |   Op(7)     |

  A signed argument is represented in excess K: the represented value is
  the written unsigned value minus K, where K is half the maximum for the
  corresponding unsigned argument.
===========================================================================*/
/*
** R[x] - register
** K[x] - constant (in constant table)
** RK(x) == if k(i) then K[x] else R[x]
*/


public enum OpCode
{
    /*----------------------------------------------------------------------
      name		args	description
    ------------------------------------------------------------------------*/
    Move,/*	A B	R[A] := R[B]					*/
    LoadI,/*	A sBx	R[A] := sBx					*/
    LoadF,/*	A sBx	R[A] := (lua_Number)sBx				*/
    LoadK,/*	A Bx	R[A] := K[Bx]					*/
    LoadKx,/*	A	R[A] := K[extra arg]				*/
    LoadFalse,/*	A	R[A] := false					*/
    LFalseSkip,/*A	R[A] := false; pc++	(*)			*/
    LoadTrue,/*	A	R[A] := true					*/
    LoadNil,/*	A B	R[A], R[A+1], ..., R[A+B] := nil		*/
    GetUpVal,/*	A B	R[A] := UpValue[B]				*/
    SetUpVal,/*	A B	UpValue[B] := R[A]				*/

    GetTabUp,/*	A B C	R[A] := UpValue[B][K[C]:shortstring]		*/
    GetTable,/*	A B C	R[A] := R[B][R[C]]				*/
    GetI,/*	A B C	R[A] := R[B][C]					*/
    GetField,/*	A B C	R[A] := R[B][K[C]:shortstring]			*/

    SetTabUp,/*	A B C	UpValue[A][K[B]:shortstring] := RK(C)		*/
    SetTable,/*	A B C	R[A][R[B]] := RK(C)				*/
    SetI,/*	A B C	R[A][B] := RK(C)				*/
    SetField,/*	A B C	R[A][K[B]:shortstring] := RK(C)			*/

    NewTable,/*	A B C k	R[A] := {}					*/

    Self,/*	A B C	R[A+1] := R[B]; R[A] := R[B][RK(C):string]	*/

    AddI,/*	A B sC	R[A] := R[B] + sC				*/

    AddK,/*	A B C	R[A] := R[B] + K[C]:number			*/
    SubK,/*	A B C	R[A] := R[B] - K[C]:number			*/
    MulK,/*	A B C	R[A] := R[B] * K[C]:number			*/
    ModK,/*	A B C	R[A] := R[B] % K[C]:number			*/
    PowK,/*	A B C	R[A] := R[B] ^ K[C]:number			*/
    DivK,/*	A B C	R[A] := R[B] / K[C]:number			*/
    IDivK,/*	A B C	R[A] := R[B] // K[C]:number			*/

    BandK,/*	A B C	R[A] := R[B] & K[C]:integer			*/
    BorK,/*	A B C	R[A] := R[B] | K[C]:integer			*/
    BxorK,/*	A B C	R[A] := R[B] ~ K[C]:integer			*/

    ShrI,/*	A B sC	R[A] := R[B] >> sC				*/
    ShlI,/*	A B sC	R[A] := sC << R[B]				*/

    Add,/*	A B C	R[A] := R[B] + R[C]				*/
    Sub,/*	A B C	R[A] := R[B] - R[C]				*/
    Mul,/*	A B C	R[A] := R[B] * R[C]				*/
    Mod,/*	A B C	R[A] := R[B] % R[C]				*/
    Pow,/*	A B C	R[A] := R[B] ^ R[C]				*/
    Div,/*	A B C	R[A] := R[B] / R[C]				*/
    IDiv,/*	A B C	R[A] := R[B] // R[C]				*/

    Band,/*	A B C	R[A] := R[B] & R[C]				*/
    Bor,/*	A B C	R[A] := R[B] | R[C]				*/
    Bxor,/*	A B C	R[A] := R[B] ~ R[C]				*/
    Shl,/*	A B C	R[A] := R[B] << R[C]				*/
    Shr,/*	A B C	R[A] := R[B] >> R[C]				*/

    MmBin,/*	A B C	call C metamethod over R[A] and R[B]	(*)	*/
    MmBinI,/*	A sB C k	call C metamethod over R[A] and sB	*/
    MmBinK,/*	A B C k		call C metamethod over R[A] and K[B]	*/

    Unm,/*	A B	R[A] := -R[B]					*/
    BNot,/*	A B	R[A] := ~R[B]					*/
    Not,/*	A B	R[A] := not R[B]				*/
    Len,/*	A B	R[A] := #R[B] (length operator)			*/

    Concat,/*	A B	R[A] := R[A].. ... ..R[A + B - 1]		*/

    Close,/*	A	close all upvalues >= R[A]			*/
    Tbc,/*	A	mark variable A "to be closed"			*/
    Jmp,/*	sJ	pc += sJ					*/
    Eq,/*	A B k	if ((R[A] == R[B]) ~= k) then pc++		*/
    Lt,/*	A B k	if ((R[A] <  R[B]) ~= k) then pc++		*/
    Le,/*	A B k	if ((R[A] <= R[B]) ~= k) then pc++		*/

    EqK,/*	A B k	if ((R[A] == K[B]) ~= k) then pc++		*/
    EqI,/*	A sB k	if ((R[A] == sB) ~= k) then pc++		*/
    LtI,/*	A sB k	if ((R[A] < sB) ~= k) then pc++			*/
    LeI,/*	A sB k	if ((R[A] <= sB) ~= k) then pc++		*/
    GtI,/*	A sB k	if ((R[A] > sB) ~= k) then pc++			*/
    GeI,/*	A sB k	if ((R[A] >= sB) ~= k) then pc++		*/

    Test,/*	A k	if (not R[A] == k) then pc++			*/
    TestSet,/*	A B k	if (not R[B] == k) then pc++ else R[A] := R[B] (*) */

    Call,/*	A B C	R[A], ... ,R[A+C-2] := R[A](R[A+1], ... ,R[A+B-1]) */
    TailCall,/*	A B C k	return R[A](R[A+1], ... ,R[A+B-1])		*/

    Return,/*	A B C k	return R[A], ... ,R[A+B-2]	(see note)	*/
    Return0,/*		return						*/
    Return1,/*	A	return R[A]					*/

    ForLoop,/*	A Bx	update counters; if loop continues then pc-=Bx; */
    ForPrep,/*	A Bx	<check values and prepare counters>;
                        if not to run then pc+=Bx+1;			*/

    TForPrep,/*	A Bx	create upvalue for R[A + 3]; pc+=Bx		*/
    TForCall,/*	A C	R[A+4], ... ,R[A+3+C] := R[A](R[A+1], R[A+2]);	*/
    TForLoop,/*	A Bx	if R[A+2] ~= nil then { R[A]=R[A+2]; pc -= Bx }	*/

    SetList,/*	A B C k	R[A][C+i] := R[A+i], 1 <= i <= B		*/

    Closure,/*	A Bx	R[A] := closure(KPROTO[Bx])			*/

    VarArg,/*	A C	R[A], R[A+1], ..., R[A+C-2] = vararg		*/

    VarArgPrep,/*A	(adjust vararg parameters)			*/

    ExtraArg/*	Ax	extra (larger) argument for previous opcode	*/
}

/*===========================================================================
  Notes:

  (*) Opcode OP_LFALSESKIP is used to convert a condition to a boolean
  value, in a code equivalent to (not cond ? false : true).  (It
  produces false and skips the next instruction producing true.)

  (*) Opcodes OP_MMBIN and variants follow each arithmetic and
  bitwise opcode. If the operation succeeds, it skips this next
  opcode. Otherwise, this opcode calls the corresponding metamethod.

  (*) Opcode OP_TESTSET is used in short-circuit expressions that need
  both to jump and to produce a value, such as (a = b or c).

  (*) In OP_CALL, if (B == 0) then B = top - A. If (C == 0), then
  'top' is set to last_result+1, so next open instruction (OP_CALL,
  OP_RETURN*, OP_SETLIST) may use 'top'.

  (*) In OP_VARARG, if (C == 0) then use actual number of varargs and
  set top (like in OP_CALL with C == 0).

  (*) In OP_RETURN, if (B == 0) then return up to 'top'.

  (*) In OP_LOADKX and OP_NEWTABLE, the next instruction is always
  OP_EXTRAARG.

  (*) In OP_SETLIST, if (B == 0) then real B = 'top'; if k, then
  real C = EXTRAARG _ C (the bits of EXTRAARG concatenated with the
  bits of C).

  (*) In OP_NEWTABLE, B is log2 of the hash size (which is always a
  power of 2) plus 1, or zero for size zero. If not k, the array size
  is C. Otherwise, the array size is EXTRAARG _ C.

  (*) For comparisons, k specifies what condition the test should accept
  (true or false).

  (*) In OP_MMBINI/OP_MMBINK, k means the arguments were flipped
   (the constant is the first operand).

  (*) All 'skips' (pc++) assume that next instruction is a jump.

  (*) In instructions OP_RETURN/OP_TAILCALL, 'k' specifies that the
  function builds upvalues, which may need to be closed. C > 0 means
  the function is vararg, so that its 'func' must be corrected before
  returning; in this case, (C - 1) is its number of fixed parameters.

  (*) In comparisons with an immediate operand, C signals whether the
  original operand was a float. (It must be corrected in case of
  metamethods.)

===========================================================================*/

/*
** masks for instruction properties. The format is:
** bits 0-2: op mode
** bit 3: instruction set register A
** bit 4: operator is a test (next instruction must be a jump)
** bit 5: instruction uses 'L->top' set by previous instruction (when B == 0)
** bit 6: instruction sets 'L->top' for next instruction (when C == 0)
** bit 7: instruction is an MM instruction (call a metamethod)
*/