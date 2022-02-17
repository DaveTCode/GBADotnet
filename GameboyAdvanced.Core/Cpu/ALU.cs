using System.Runtime.CompilerServices;

namespace GameboyAdvanced.Core.Cpu;

internal static class ALU
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetZeroSignFlags(ref CPSR cpsr, uint result)
    {
        cpsr.SignFlag = ((result >> 31) & 1) == 1;
        cpsr.ZeroFlag = result == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetZeroSignFlags(ref CPSR cpsr, ulong result)
    {
        cpsr.SignFlag = ((result >> 63) & 1) == 1;
        cpsr.ZeroFlag = result == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ADD(uint op1, uint op2, ref CPSR cpsr)
    {
        var result = (long)op1 + op2;

        SetZeroSignFlags(ref cpsr, (uint)result);
        cpsr.CarryFlag = result >> 32 == 1;
        cpsr.OverflowFlag = ((op1 ^ (uint)result) & (op2 ^ (uint)result) & 0x8000_0000) != 0;

        return (uint)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ADC(uint op1, uint op2, ref CPSR cpsr)
    {
        var result = (long)op1 + op2 + (cpsr.CarryFlag ? 1 : 0);

        SetZeroSignFlags(ref cpsr, (uint)result);
        cpsr.CarryFlag = result >> 32 == 1;
        cpsr.OverflowFlag = ((op1 ^ (uint)result) & (op2 ^ (uint)result) & 0x8000_0000) != 0;

        return (uint)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint SUB(uint op1, uint op2, ref CPSR cpsr)
    {
        var result = op1 - op2;

        SetZeroSignFlags(ref cpsr, result);
        cpsr.CarryFlag = op1 >= op2;
        cpsr.OverflowFlag = (op1 & 0x8000_0000) != (op2 & 0x8000_0000) && (op1 & 0x8000_0000) != (result & 0x8000_0000);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint SBC(uint op1, uint op2, ref CPSR cpsr)
    {
        var result = op1 - op2 - (cpsr.CarryFlag ? 0 : 1);

        SetZeroSignFlags(ref cpsr, (uint)result);
        cpsr.CarryFlag = op1 >= op2;
        cpsr.OverflowFlag = (op1 & 0x8000_0000) != (op2 & 0x8000_0000) && (op1 & 0x8000_0000) != (result & 0x8000_0000);

        return (uint)result;
    }
}
