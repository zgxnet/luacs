using Lua.Internal;
using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

namespace Lua.Runtime;

public static partial class LuaVM
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Execute(LuaState L, int nargs, int nresults, int errfunc = 0)
    {
        ref LuaValue val = ref L.stack.GetTopRef();
        LuaClosure closure = val.AsLuaClosure();
        Proto proto = closure.proto;
        int func = L.stack.Top - nargs - 1; //the function is at the top of the stack
        int top = func + 1 + proto.MaxStackSize;
        L.callStack.Push(new CallInfo
        {
            func = func,
            top = top,
            savedpc = 0,
            nresults = nresults,
        });
        ExecutionContext context = new(L);
        context.DoExecute();
    }

    static LuaValue FastGet(in LuaValue t, int key)
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void DoGetTable(LuaTable table, in LuaValue key, /*out*/ref LuaValue val)
    {
        table.DoGet(in key, ref val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool Compare(in LuaValue a, in LuaValue b)
    {
        LuaValueType type = a.type;
        if (type != b.type)
            return false;
        if (a.ivalue != b.ivalue)
            return false;
        if (type == LuaValueType.String)
            return a.UnsafeAsString() == b.UnsafeAsString();
        else
            return ReferenceEquals(a.rvalue, b.rvalue);
    }

    static LuaValue FastGet(in LuaValue t, string key)
    {
        if (!t.IsTable)
            return default;
        var table = t.UnsafeAsTable();
        if (table.dict is null)
            return default;
        return table.dict[key];
    }

    static bool FastSet(in LuaValue t, string key, in LuaValue v)
    {
        if (!t.IsTable)
            return false;
        var table = t.UnsafeAsTable();
        if (table.dict is null)
            return false;
        table.dict[key] = v;
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static void DoSetTable(LuaTable table, int key, in LuaValue v)
    {
        table.DoSet(key, in v);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static void DoSetTable(LuaTable table, in LuaValue key, in LuaValue v)
    {
        table.DoSet(in key, in v);
    }

    static LuaClosure CreateClosure()
    {
        throw new NotImplementedException();
    }

    /*
    ** Rounding modes for float->integer coercion
     */
    enum F2Imod
    {
        Eq,     /* no rounding; accepts only integral values */
        Floor,  /* takes the floor of the number */
        Ceil    /* takes the ceil of the number */
    }

    static bool ToInteger(double v, out long p, F2Imod mode)
    {
        double v1 = mode switch
        {
            F2Imod.Floor => Math.Floor(v), //round down
            F2Imod.Ceil => Math.Ceiling(v), //round up
            _ => v
        };
        long p1 = (long)v1;
        if (p1 == v1)
        {
            p = p1;
            return true;
        }
        else
        {
            p = 0;
            return false;
        }
    }

    /*
    ** try to convert a value to an integer, rounding according to 'mode',
    ** without string coercion.
    ** ("Fast track" handled by macro 'tointegerns'.)
    */
    static bool ToIntegerNS(in LuaValue obj, out long p, F2Imod mode) {
        if (obj.IsFloat)
            return ToInteger(obj.UnsafeAsDouble(), out p, mode);
        else if (obj.IsInteger)
        {
            p = obj.UnsafeAsInteger();
            return true;
        }
        else
        {
            p = 0;
            return false;
        }
    }

    /*
    ** try to convert a value to an integer.
    */
    static bool ToInteger(in LuaValue obj, out long p, F2Imod mode) {
        if (obj.IsString)
        {
            string s = obj.UnsafeAsString();
            if (long.TryParse(s, out p))
                return true;
            else if (double.TryParse(s, out var p1))
                return ToInteger(p1, out p, mode);
            else
            {
                p = 0;
                return false;
            }
        }
        else
            return ToIntegerNS(obj, out p, mode); //fast track for non strings
    }

    /*
    ** Main operation 'ra = #rb'.
    *see: void luaV_objlen (lua_State *L, StkId ra, const TValue *rb) {
    */
    static long GetObjLen(in LuaValue ra)
    {
        switch(ra.type)
        {
            case LuaValueType.Table:
                return ra.UnsafeAsTable().GetLength();
            default:
                throw new LuaException("Attempt to get length of a non-table value: " + ra.type);
        }
    }

    ref partial struct ExecutionContext
    {
        //call stack
        ref FastStackCore<CallInfo> callStack;
        ref CallInfo ci;

        //stack
        ref FastStackCore<LuaValue> stack;
        ref int stackTop;
        ref LuaValue sabase;
        ref LuaValue sbase;

        //thread
        LuaState thread;

        //ci
        LuaClosure closure;
        Proto proto;

        //res
        ref LuaValue kbase;
        ref UpValue ubase;

        public ExecutionContext(LuaState L)
        {
            //call stack
            callStack = ref L.callStack;
            ci = ref callStack.GetTopRef();

            //stack
            stack = ref L.stack;
            stackTop = ref stack.top;

            //thread
            thread = L;

            if ((uint)ci.func >= ci.top)
                throw new LuaException("Function origin out of bounds.");

            UpdateCurFunc();
        }

        [MemberNotNull(nameof(closure), nameof(proto), nameof(kbase), nameof(ubase))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdateCurFunc()
        {
            //ci
            closure = stack[ci.func].AsLuaClosure();
            proto = closure.Proto;

            //pc
            if ((uint)ci.savedpc >= proto.Code.Length)
                throw new LuaException("PC index out of bounds.");

            //stack
            sabase = ref stack.GetFirstRef();
            sbase = ref Unsafe.Add(ref sabase, ci.func + 1);

            //res            
            kbase = ref MemoryMarshal.GetArrayDataReference(proto.K);
            ubase = ref MemoryMarshal.GetArrayDataReference(closure.upValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool StackReserveMore(int cap)
        {
            stack.ReserveMore(LuaConstants.LuaMinStack, out bool realloced);
            if (realloced)
            {
                sabase = ref stack.GetFirstRef();
                sbase = ref Unsafe.Add(ref sabase, ci.func + 1);
            }
            return realloced;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ref CallInfo PushAndRefCI()
        {
            ref var ci1 = ref callStack.APushAndRef(out bool realloced);
            if (realloced)
            {
                ci = ref Unsafe.Subtract(ref ci1, 1);
            }
            return ref ci1;
        }

        void CallCFunction(ref LuaValue rA, int A, int B, int C) //R[A], ... ,R[A+C-2] := R[A](R[A+1], ... ,R[A+B-1])
        {
            int func = ci.func + A + 1;
            if (B > 0) stackTop = func + B;
            //prepare new stack
            if (StackReserveMore(LuaConstants.LuaMinStack))
            {
                rA = ref Unsafe.Add(ref sabase, func);
            }
            //prapare new frame
            callStack.APush(new CallInfo
            {
                func = func,
                top = stackTop + LuaConstants.LuaMinStack,
                savedpc = 0, //will be set later
                nresults = C - 1,
            });
            ci = ref callStack.GetTopRef();
            //call the function
            LuaCFunction fptr = rA.AsCFunction();
            int nres = fptr(thread); //real call
            MoveResults(func, nres, ci.nresults);
            callStack.Pop();
            ci = ref callStack.GetTopRef();
        }

        //func .. stackTop-1: the previous function stack
        //nres: the number of results to return, -1 for all
        //wanted: the number of results wanted
        //min(wanted, nres) results will be moved to func ..., wanted-min(wanted, nres) will be cleared
        void MoveResults(int func, int nres, int wanted)
        {
            if (wanted < 0) wanted = nres; //all results
            nres = Math.Min(Math.Max(nres, 0), stackTop - func);
            int n = Math.Min(nres, wanted);
            //move n results: stackTop-n .. stackTop-1 -> func .. func+n-1
            if (n > 0)
            {
                Span<LuaValue> src = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref sabase, stackTop - n), n);
                Span<LuaValue> dst = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref sabase, func), n);
                src.CopyTo(dst);
            }
            //fill nil
            if (n < wanted)
            {
                Span<LuaValue> dst = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref sabase, func + n), wanted - n);
                dst.Clear();
            }
            stack.PopUntil(func + wanted);
        }

        //old: func, fixarg0, fixarg1, ..., fixargn, vararg0, vararg1, ...varargm
        //new:                                       vararg0, vararg1, ...varargm, func, fixarg0, fixarg1, ..., fixargn
        void AdjustVarArgs(int nfixparams, ref CallInfo ci, Proto p)
        {
            StackReserveMore(p.MaxStackSize + 1);
            int actual = stackTop - ci.func - 1;
            ci.nextraargs = actual - nfixparams;
            Debug.Assert(ci.nextraargs >= 0);
            Span<LuaValue> src = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref sabase, ci.func), nfixparams + 1);
            Span<LuaValue> dst = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref sabase, stackTop), nfixparams + 1);
            src.CopyTo(dst);
            src.Clear();
            ci.func += actual + 1;
            ci.top += actual + 1;
            stackTop += nfixparams + 1;
            sbase = ref Unsafe.Add(ref sabase, ci.func + 1);
        }

        /*
        ** Try to convert a 'for' limit to an integer, preserving the semantics
        ** of the loop. Return true if the loop must not run; otherwise, '*p'
        ** gets the integer limit.
        ** (The following explanation assumes a positive step; it is valid for
        ** negative steps mutatis mutandis.)
        ** If the limit is an integer or can be converted to an integer,
        ** rounding down, that is the limit.
        ** Otherwise, check whether the limit can be converted to a float. If
        ** the float is too large, clip it to LUA_MAXINTEGER.  If the float
        ** is too negative, the loop should not run, because any initial
        ** integer value is greater than such limit; so, the function returns
        ** true to signal that. (For this latter case, no integer limit would be
        ** correct; even a limit of LUA_MININTEGER would run the loop once for
        ** an initial value equal to LUA_MININTEGER.)
        */
        static bool ForLimit(long init, in LuaValue lim, out long p, long step)
        {
            if (!ToInteger(lim, out p, (step < 0 ? F2Imod.Ceil : F2Imod.Floor)))
            {
                /* not coercible to in integer */
                if (!lim.TryToNumber(out double flim))  /* try to convert to float */
                    throw new LuaException($"Cannot convert {lim} to number");
                /* else 'flim' is a float out of integer bounds */
                if (flim > 0)
                {  /* if it is positive, it is too large */
                    if (step < 0) return true;  /* initial value must be less than it */
                    p = long.MaxValue;  /* truncate */
                }
                else
                {  /* it is less than min integer */
                    if (step > 0) return true;  /* initial value must be greater than it */
                    p = long.MinValue;  /* truncate */
                }
            }
            return (step > 0 ? init > p : init < p);  /* not to run? */
        }

        /*
        ** Prepare a numerical for loop (opcode OP_FORPREP).
        ** Return true to skip the loop. Otherwise,
        ** after preparation, stack will be as follows:
        **   ra : internal index (safe copy of the control variable)
        **   ra + 1 : loop counter (integer loops) or limit (float loops)
        **   ra + 2 : step
        **   ra + 3 : control variable
        *see: static int forprep (lua_State *L, StkId ra)
        */
        static bool ForPrep(ref LuaValue ra)
        {
            ref LuaValue pinit = ref ra;
            ref LuaValue plimit = ref Unsafe.Add(ref ra, 1);
            ref LuaValue pstep = ref Unsafe.Add(ref ra, 2);
            if(pinit.IsInteger && pstep.IsInteger) //integer loop?
            {
                long init = pinit.UnsafeAsInteger();
                long step = pstep.UnsafeAsInteger();
                if(step == 0)
                {
                    //step is zero, error
                    throw new LuaException("'for' step is zero");
                }
                Unsafe.Add(ref ra, 3) = init; //control variable
                if (ForLimit(init, plimit, out long limit, step))
                    return true;  /* skip the loop */
                else
                {  /* prepare loop counter */
                    ulong count;
                    if (step > 0)
                    {  /* ascending loop? */
                        count = (ulong)limit - (ulong)init;
                        if (step != 1)  /* avoid division in the too common case */
                            count /= (ulong)step;
                    }
                    else
                    {  /* step < 0; descending loop */
                        count = (ulong)init - (ulong)limit;
                        /* 'step+1' avoids negating 'mininteger' */
                        count /= (ulong)(-(step + 1)) + 1u;
                    }
                    /* store the counter in place of the limit (which won't be
                       needed anymore) */
                    plimit = (long)count;
                }
            }
            else
            {
                throw new NotImplementedException("Float for loop is not implemented yet.");
            }
            return false;
        }
    }
}
