using Lua.Internal;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lua.Runtime;
partial class LuaVM
{
    partial struct ExecutionContext
    {
        //see luaV_execute
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void DoExecute()
        {
            ref Instruction pc = ref proto.Code[ci.savedpc];
            int baseCIndex = callStack.Count;
            ref LuaValue sbase = ref this.sbase;

            while (true)
            {
                uint instruction = pc.Value;
                pc = ref Unsafe.Add(ref pc, 1);
                OpCode opCode = ((OpCode)(instruction & 0x7F));
                switch(opCode)
                {
                    case OpCode.Move: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue rB = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 16) & 0xFF)));
                        if (rB.rvalue == null)
                        {
                            rA.type = rB.type;
                            rA.ivalue = rB.ivalue;
                            rA.rvalue = null;
                        }
                        else
                        {
                            rA = rB;
                        }
                    } break;
                    case OpCode.LoadI: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        int sBx = ((int)(((int)((instruction >> 15) & 0x1FFFF))-65535));
                        rA.Set(sBx);
                    } break;
                    case OpCode.LoadF: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        int sBx = ((int)(((int)((instruction >> 15) & 0x1FFFF))-65535));
                        rA.Set((double)sBx);
                    } break;
                    case OpCode.LoadK: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        int Bx = ((int)((instruction >> 15) & 0x1FFFF));
                        rA = Unsafe.Add(ref kbase, Bx);
                    } break;
                    case OpCode.LoadKx: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        rA = Unsafe.Add(ref kbase, pc.Ax);
                        pc = ref Unsafe.Add(ref pc, 1);
                    } break;
                    case OpCode.LoadFalse: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        rA = new LuaValue(false);
                    } break;
                    case OpCode.LFalseSkip: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        rA = new LuaValue(false);
                        pc = ref Unsafe.Add(ref pc, 1);
                    } break;
                    case OpCode.LoadTrue: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        rA = new LuaValue(true);
                    } break;
                    case OpCode.LoadNil: {
                        int A = ((byte)((instruction >> 7)));
                        int B = ((byte)((instruction >> 16) & 0xFF));
                        MemoryMarshal.CreateSpan(ref Unsafe.Add(ref sbase, A) , B+1).Clear();
                    } break;
                    case OpCode.GetUpVal: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue uB = ref Unsafe.Add(ref ubase, ((byte)((instruction >> 16) & 0xFF))).GetRefValue();
                        rA = uB;
                    } break;
                    case OpCode.SetUpVal: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue uB = ref Unsafe.Add(ref ubase, ((byte)((instruction >> 16) & 0xFF))).GetRefValue();
                        uB = rA;
                    } break;
                    case OpCode.GetTabUp: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue uB = ref Unsafe.Add(ref ubase, ((byte)((instruction >> 16) & 0xFF))).GetRefValue();
                        ref LuaValue kC = ref Unsafe.Add(ref kbase, ((byte)((instruction >> 24) & 0xFF)));
                        string key = kC.UnsafeAsString();
                        rA = FastGet(uB, key);
                    } break;
                    case OpCode.GetTable: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue rB = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 16) & 0xFF)));
                        ref LuaValue rC = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 24) & 0xFF)));
                        if (!(rB.type == LuaValueType.Table))
                        {
                            throw new Exception($"Expected table at R[B], but got {rB.Type}.");
                        }
                        else
                        {
                            DoGetTable(Unsafe.As<LuaTable>(rB.rvalue!), in rC, ref rA);
                        }
                    } break;
                    case OpCode.GetI: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue rB = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 16) & 0xFF)));
                        int C = ((byte)((instruction >> 24) & 0xFF));
                        rA = FastGet(rB, C);
                    } break;
                    case OpCode.NewTable: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        int B = ((byte)((instruction >> 16) & 0xFF));
                        int C = ((byte)((instruction >> 24) & 0xFF));
                        bool K = (((instruction >> 15) & 1) > 0);
                        if(B > 0) B = 1 << (B-1);
                        if(K) C += pc.Ax * (Instruction.MAXARG_C + 1);  /* add it to size */
                        pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        rA = new LuaTable(B, C);
                    } break;
                    case OpCode.GetField: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue rB = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 16) & 0xFF)));
                        ref LuaValue kC = ref Unsafe.Add(ref kbase, ((byte)((instruction >> 24) & 0xFF)));
                        string key = kC.UnsafeAsString();
                        rA = FastGet(rB, key);
                    } break;
                    case OpCode.SetTabUp: {
                        ref LuaValue uA = ref Unsafe.Add(ref ubase, ((byte)((instruction >> 7)))).GetRefValue();
                        ref LuaValue kB = ref Unsafe.Add(ref kbase, ((byte)((instruction >> 16) & 0xFF)));
                        ref LuaValue rkC = ref (((instruction >> 15) & 1) > 0) ? ref Unsafe.Add(ref kbase, ((byte)((instruction >> 24) & 0xFF))) : ref Unsafe.Add(ref sbase, ((byte)((instruction >> 24) & 0xFF)));
                        string key = kB.UnsafeAsString();
                        FastSet(uA, key, rkC);
                    } break;
                    case OpCode.Closure: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        int Bx = ((int)((instruction >> 15) & 0x1FFFF));
                        Proto p = proto.P[Bx];
                        rA = new LuaClosure(thread, p);
                    } break;
                    case OpCode.Call: {
                        int A = ((byte)((instruction >> 7)));
                        int B = ((byte)((instruction >> 16) & 0xFF));
                        int C = ((byte)((instruction >> 24) & 0xFF));
                        //A B C	R[A], ... ,R[A+C-2] := R[A](R[A+1], ... ,R[A+B-1])
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, A);
                        if (rA.IsCFunction)
                        {
                            CallCFunction(ref rA, A, B, C);
                            sbase = ref this.sbase;
                        }
                        else
                        {
                            ref var ci1 = ref PushAndRefCI();
                            LuaClosure closure = rA.AsLuaClosure();
                            Proto proto = closure.Proto;
                            //save pc
                            ci.savedpc = (int)(Unsafe.ByteOffset(ref MemoryMarshal.GetArrayDataReference(this.proto.Code), ref pc) / Unsafe.SizeOf<Instruction>());
                            //new ci
                            int func = ci.func + A + 1;
                            ci1.func = func;
                            ci1.top = func + proto.MaxStackSize + 1;
                            ci1.nresults = C - 1; //wanted results, set top=last_result+1 when C=0
                            ci1.nextraargs = 0; //defer to vararg prep
                            ci1.savedpc = 0; //reset PC
                            //stack
                            if (B == 0)
                            {
                                int n = stackTop - ci.func - 1 - A;
                                int gap = proto.NumParams - n;
                                if (gap > 0)
                                {
                                    if(StackReserveMore(gap))
                                    {
                                        sbase = ref this.sbase;
                                        rA = ref Unsafe.Add(ref sbase, A);
                                    }
                                    MemoryMarshal.CreateSpan(ref Unsafe.Add(ref sbase, stackTop), gap).Clear();
                                    stackTop += gap;
                                }
                            }
                            else
                            { //B>0
                                int n = B - 1;
                                int gap = proto.NumParams - n;
                                if (gap > 0)
                                {
                                    if(StackReserveMore(gap))
                                    {
                                        sbase = ref this.sbase;
                                        rA = ref Unsafe.Add(ref sbase, A);
                                    }
                                    MemoryMarshal.CreateSpan(ref Unsafe.Add(ref rA, B), gap).Clear();
                                    B += gap;
                                }
                                stackTop = func + B;
                            }
                            ci = ref ci1;
                            //pc
                            pc = ref MemoryMarshal.GetArrayDataReference(proto.Code);
                            stack.EnsureCapacity(ci.top);
                            UpdateCurFunc();
                            sbase = ref this.sbase;
                        }
                    } break;
                    case OpCode.VarArgPrep: {
                        int A = ((byte)((instruction >> 7)));
                        AdjustVarArgs(A, ref ci, proto);
                        sbase = ref this.sbase;
                    } break;
                    case OpCode.Return: {
                        int A = ((byte)((instruction >> 7)));
                        int B = ((byte)((instruction >> 16) & 0xFF));
                        int C = ((byte)((instruction >> 24) & 0xFF));
                        bool K = (((instruction >> 15) & 1) > 0);
                        //A B C k	return R[A], ... ,R[A+B-2],
                        //if (B == 0) then return up to 'top'
                        //C > 0 means the function is vararg, so that its 'func' must be corrected before returning; in this case, (C - 1) is its number of fixed parameters.
                        int nres;
                        if (B > 0) //B == 0 means return all results
                        {
                            stackTop = ci.func + A + B;
                            nres = B - 1;
                        }
                        else
                        {
                            nres = stackTop - (ci.func + A + 1);
                        }
                        if (K) //build up values
                        {
                            throw new NotImplementedException();
                        }
                        if (C > 0)
                        {
                            ci.func -= ci.nextraargs + C;
                        }
                        int wanted = ci.nresults;
                        MoveResults(ci.func, nres, wanted);
                        ref var prevCI = ref Unsafe.Subtract(ref ci, 1);
                        callStack.Pop();
                        ci = ref prevCI;
                        if (callStack.Count >= baseCIndex)
                        {
                            UpdateCurFunc();
                            pc = ref proto.Code[ci.savedpc];
                            sbase = ref this.sbase;
                        }
                        else
                            return; //return to caller
                    } break;
                    case OpCode.Return0: {
                        int wanted = ci.nresults;
                        MoveResults(ci.func, 0, wanted);
                        ref var prevCI = ref Unsafe.Subtract(ref ci, 1);
                        callStack.Pop();
                        ci = ref prevCI;
                        if (callStack.Count >= baseCIndex)
                        {
                            UpdateCurFunc();
                            pc = ref proto.Code[ci.savedpc];
                            sbase = ref this.sbase;
                        }
                        else
                            return; //return to caller
                    } break;
                    case OpCode.Return1: {
                        int A = ((byte)((instruction >> 7)));
                        stackTop = ci.func + A + 2;
                        int wanted = ci.nresults;
                        MoveResults(ci.func, 1, wanted);
                        ref var prevCI = ref Unsafe.Subtract(ref ci, 1);
                        callStack.Pop();
                        ci = ref prevCI;
                        if (callStack.Count >= baseCIndex)
                        {
                            UpdateCurFunc();
                            pc = ref proto.Code[ci.savedpc];
                            sbase = ref this.sbase;
                        }
                        else
                            return; //return to caller
                    } break;
                    case OpCode.Jmp: {
                        int sJ = (((int)((instruction >> 7) & 0x3FFFFFF))-16777215);
                        pc = ref Unsafe.Add(ref pc, sJ);
                    } break;
                    case OpCode.Eq: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue rB = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 16) & 0xFF)));
                        bool K = (((instruction >> 15) & 1) > 0);
                        if ((rA.type == LuaValueType.Integer) && (rB.type == LuaValueType.Integer))
                        {
                            long a = (rA.ivalue);
                            long b = (rB.ivalue);
                            if ((a == b) != K)
                                pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        }
                        else if ((rA.type == LuaValueType.Float) && (rB.type == LuaValueType.Float))
                        {
                            double a = (rA.fvalue);
                            double b = (rB.fvalue);
                            if ((a == b) != K)
                                pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        }
                        else if (rA.IsNumber && rB.IsNumber)
                        {
                            double a = rA.IsFloat ? (rA.fvalue) : (double)(rA.ivalue);
                            double b = rB.IsFloat ? (rB.fvalue) : (double)(rB.ivalue);
                            if ((a == b) != K)
                                pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        } else
                            throw new NotImplementedException($"Arith == not implemented for {rA.Type} and {rB.Type}.");
                    } break;
                    case OpCode.Lt: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue rB = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 16) & 0xFF)));
                        bool K = (((instruction >> 15) & 1) > 0);
                        if ((rA.type == LuaValueType.Integer) && (rB.type == LuaValueType.Integer))
                        {
                            long a = (rA.ivalue);
                            long b = (rB.ivalue);
                            if ((a < b) != K)
                                pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        }
                        else if ((rA.type == LuaValueType.Float) && (rB.type == LuaValueType.Float))
                        {
                            double a = (rA.fvalue);
                            double b = (rB.fvalue);
                            if ((a < b) != K)
                                pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        }
                        else if (rA.IsNumber && rB.IsNumber)
                        {
                            double a = rA.IsFloat ? (rA.fvalue) : (double)(rA.ivalue);
                            double b = rB.IsFloat ? (rB.fvalue) : (double)(rB.ivalue);
                            if ((a < b) != K)
                                pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        } else
                            throw new NotImplementedException($"Arith < not implemented for {rA.Type} and {rB.Type}.");
                    } break;
                    case OpCode.Le: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue rB = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 16) & 0xFF)));
                        bool K = (((instruction >> 15) & 1) > 0);
                        if ((rA.type == LuaValueType.Integer) && (rB.type == LuaValueType.Integer))
                        {
                            long a = (rA.ivalue);
                            long b = (rB.ivalue);
                            if ((a <= b) != K)
                                pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        }
                        else if ((rA.type == LuaValueType.Float) && (rB.type == LuaValueType.Float))
                        {
                            double a = (rA.fvalue);
                            double b = (rB.fvalue);
                            if ((a <= b) != K)
                                pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        }
                        else if (rA.IsNumber && rB.IsNumber)
                        {
                            double a = rA.IsFloat ? (rA.fvalue) : (double)(rA.ivalue);
                            double b = rB.IsFloat ? (rB.fvalue) : (double)(rB.ivalue);
                            if ((a <= b) != K)
                                pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        } else
                            throw new NotImplementedException($"Arith <= not implemented for {rA.Type} and {rB.Type}.");
                    } break;
                    case OpCode.Concat: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        int B = ((byte)((instruction >> 16) & 0xFF));
                        //A B	R[A] := R[A].. ... ..R[A + B - 1]
                        if (B == 0)
                        {
                            rA = "";
                        }else if(B > 1)
                        {
                            StringBuilder sb = new StringBuilder(rA.ToString());
                            for (int i = 1; i < B; i++)
                                sb.Append(Unsafe.Add(ref rA, i));
                            rA = sb.ToString();
                        }
                    } break;
                    case OpCode.ForPrep: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        if(ForPrep(ref rA))
                            pc = ref Unsafe.Add(ref pc, ((int)((instruction >> 15) & 0x1FFFF)) + 1);
                    } break;
                    case OpCode.ForLoop: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue pcount = ref Unsafe.Add(ref rA, 1);
                        ref LuaValue pstep = ref Unsafe.Add(ref rA, 2);
                        ref LuaValue pctrl = ref Unsafe.Add(ref rA, 3);
                        if ((pstep.type == LuaValueType.Integer)) //all integer or all float
                        {
                            ulong count = (ulong)(pcount.ivalue);
                            if (count > 0)
                            {
                                long step = (pstep.ivalue);
                                long idx = (rA.ivalue);
                                pcount.ivalue = (long)(count - 1);
                                idx = idx + step;
                                rA.ivalue = idx;
                                pctrl.ivalue = idx;
                                pc = ref Unsafe.Subtract(ref pc, ((int)((instruction >> 15) & 0x1FFFF)));
                            }
                        }
                    } break;
                    case OpCode.Len: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue rB = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 16) & 0xFF)));
                        rA.Set(GetObjLen(rB));
                    } break;
                    case OpCode.SetI: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        int B = ((byte)((instruction >> 16) & 0xFF));
                        ref LuaValue rkC = ref (((instruction >> 15) & 1) > 0) ? ref Unsafe.Add(ref kbase, ((byte)((instruction >> 24) & 0xFF))) : ref Unsafe.Add(ref sbase, ((byte)((instruction >> 24) & 0xFF)));
                        if(!(rA.type == LuaValueType.Table))
                        {
                            throw new Exception($"Expected table at R[A], but got {rA.Type}.");
                        }
                        else
                            DoSetTable(Unsafe.As<LuaTable>(rA.rvalue!), B, in rkC);
                    } break;
                    case OpCode.SetList: {
                        int A = ((byte)((instruction >> 7)));
                        int B = ((byte)((instruction >> 16) & 0xFF));
                        int C = ((byte)((instruction >> 24) & 0xFF));
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, A);
                        if (B == 0)
                        {
                            B = stackTop - ci.func - 2 - A;
                            Debug.Assert(B > 0);
                        }
                        if ((((instruction >> 15) & 1) > 0))
                        {
                            throw new NotImplementedException("SetList with K flag is not implemented.");
                        }
                        if (!(rA.type == LuaValueType.Table))
                            throw new Exception($"Expected table at R[{A}], but got {rA.Type}.");
                        LuaTable table = Unsafe.As<LuaTable>(rA.rvalue!);
                        for (int i = 1; i <= B; i++)
                        {
                            DoSetTable(table, C + i, in Unsafe.Add(ref rA, i));
                        }
                    } break;
                    case OpCode.SetTable: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue rB = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 16) & 0xFF)));
                        ref LuaValue rkC = ref (((instruction >> 15) & 1) > 0) ? ref Unsafe.Add(ref kbase, ((byte)((instruction >> 24) & 0xFF))) : ref Unsafe.Add(ref sbase, ((byte)((instruction >> 24) & 0xFF)));
                        //A B C	R[A][R[B]] := RK(C)
                        if (!(rA.type == LuaValueType.Table))
                        {
                            throw new Exception($"Expected table at R[A], but got {rA.Type}.");
                        }
                        else
                        {
                            DoSetTable(Unsafe.As<LuaTable>(rA.rvalue!), in rB, in rkC);
                        }
                    } break;
                    case OpCode.Test: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        bool K = (((instruction >> 15) & 1) > 0);
                        //A k	if (not R[A] == k) then pc++
                        bool isFalse = rA.IsNil || (rA.IsBool && !rA.bvalue);
                        if(isFalse == K)
                        {
                            pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        }
                    } break;
                    case OpCode.AddI: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue rB = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 16) & 0xFF)));
                        int sC = (((byte)((instruction >> 24) & 0xFF))-127);
                        //R[A] := R[B] + sC
                        if ((rB.type == LuaValueType.Integer))
                        {
                            long b = (rB.ivalue);
                            rA.Set(b + sC);
                            pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        }
                        else if ((rB.type == LuaValueType.Float))
                        {
                            double b = (rB.fvalue);
                            rA.Set(b + sC);
                            pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        }
                        else
                        {
                            throw new NotImplementedException($"AddI not implemented for {rB.Type}.");
                        }
                    } break;
                    case OpCode.Add: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue rB = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 16) & 0xFF)));
                        ref LuaValue rC = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 24) & 0xFF)));
                        if ((rB.type == LuaValueType.Integer) && (rC.type == LuaValueType.Integer))
                        {
                            long b = (rB.ivalue);
                            long c = (rC.ivalue);
                            rA.Set(b + c);
                            pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        }
                        else if ((rB.type == LuaValueType.Float) && (rC.type == LuaValueType.Float))
                        {
                            double b = (rB.fvalue);
                            double c = (rC.fvalue);
                            rA.Set(b + c);
                            pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        }
                        else if (rB.IsNumber && rC.IsNumber)
                        {
                            double b = rB.IsFloat ? (rB.fvalue) : (double)(rB.ivalue);
                            double c = rC.IsFloat ? (rC.fvalue) : (double)(rC.ivalue);
                            rA.Set(b + c);
                            pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        } else
                            throw new NotImplementedException($"Arith + not implemented for {rA.Type} and {rB.Type}.");
                    } break;
                    case OpCode.Sub: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue rB = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 16) & 0xFF)));
                        ref LuaValue rC = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 24) & 0xFF)));
                        if ((rB.type == LuaValueType.Integer) && (rC.type == LuaValueType.Integer))
                        {
                            long b = (rB.ivalue);
                            long c = (rC.ivalue);
                            rA.Set(b - c);
                            pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        }
                        else if ((rB.type == LuaValueType.Float) && (rC.type == LuaValueType.Float))
                        {
                            double b = (rB.fvalue);
                            double c = (rC.fvalue);
                            rA.Set(b - c);
                            pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        }
                        else if (rB.IsNumber && rC.IsNumber)
                        {
                            double b = rB.IsFloat ? (rB.fvalue) : (double)(rB.ivalue);
                            double c = rC.IsFloat ? (rC.fvalue) : (double)(rC.ivalue);
                            rA.Set(b - c);
                            pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        } else
                            throw new NotImplementedException($"Arith - not implemented for {rA.Type} and {rB.Type}.");
                    } break;
                    case OpCode.EqK: {
                        ref LuaValue rA = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 7))));
                        ref LuaValue rB = ref Unsafe.Add(ref sbase, ((byte)((instruction >> 16) & 0xFF)));
                        bool K = (((instruction >> 15) & 1) > 0);
                        //A B k	if ((R[A] == K[B]) ~= k) then pc++
                        if (Compare(in rA, in rB) != K)
                        {
                            pc = ref Unsafe.Add(ref pc, 1); //skip next instruction
                        }
                    } break;
                    default: {
                        throw new NotImplementedException($"OpCode {opCode} is not implemented.");
                    }
                }
            }
        }
    }
}
