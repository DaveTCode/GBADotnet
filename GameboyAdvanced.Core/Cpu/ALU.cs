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
    internal static uint LSL(uint op1, byte offset, ref CPSR cpsr)
    {
        var (result, carry) = offset switch
        {
            // "LSL#0 performs no shift (the carry flag remains unchanged)"
            0 => (op1, cpsr.CarryFlag),
            _ when offset < 32 => (op1 << offset, ((op1 >> (32 - offset)) & 1) == 1),
            32 => (0u, (op1 & 1) == 1),
            _ => (0u, false),
        };

        cpsr.CarryFlag = carry;
        SetZeroSignFlags(ref cpsr, result);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint LSLNoFlags(uint op1, byte offset) => offset switch
    {
        // "LSL#0 performs no shift (the carry flag remains unchanged)"
        0 => op1,
        _ when offset < 32 => op1 << offset,
        _ => 0u,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint LSR(uint op1, byte offset, ref CPSR cpsr)
    {
        var (result, carry) = offset switch
        {
            // "LSR#0 is translated as LSR#32"
            0 => (0u, ((op1 >> 31) & 1) == 1),
            _ when offset < 32 => (op1 >> offset, ((op1 >> (offset - 1)) & 1) == 1),
            32 => (0u, ((op1 >> 31) & 1) == 1),
            _ => (0u, false),
        };

        cpsr.CarryFlag = carry;
        SetZeroSignFlags(ref cpsr, result);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint LSRNoFlags(uint op1, byte offset) => offset switch
    {
        // "LSR#0 is translated as LSR#32"
        0 => 0u,
        _ when offset < 32 => op1 >> offset,
        _ => 0u,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ASR(uint op1, byte offset, ref CPSR cpsr)
    {
        // "ASR#0 is translated as ASR#32"
        offset = (offset == 0) ? (byte)32 : offset;

        var (result, carry) = offset switch
        {
            _ when offset < 32 => ((uint)((int)op1 >> offset), ((op1 >> (offset - 1)) & 1) == 1),
            _ => (((op1 >> 31) & 1) == 1 ? uint.MaxValue : 0, ((op1 >> 31) & 1) == 1),
        };

        cpsr.CarryFlag = carry;
        SetZeroSignFlags(ref cpsr, result);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ASRNoFlags(uint op1, byte offset)
    {
        // "ASR#0 is translated as ASR#32"
        offset = (offset == 0) ? (byte)32 : offset;

        var result = offset switch
        {
            _ when offset < 32 => (uint)((int)op1 >> offset),
            _ => ((op1 >> 31) & 1) == 1 ? uint.MaxValue : 0,
        };

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ROR(uint op1, byte offset, ref CPSR cpsr)
    {
        if (offset > 32) offset = (byte)(offset % 32);

        var (result, carry) = offset switch
        {
            0 => (0u, false),
            _ when offset < 32 => 
            (
                ((op1) >> (offset)) | ((op1) << (32 - offset)),
                ((op1 >> (offset - 1)) & 1) == 1
            ),
            32 => (op1, ((op1 >> 31) & 1) == 1),
            _ => throw new Exception("Invalid value for ROR offset"),
        };

        cpsr.CarryFlag = carry;
        SetZeroSignFlags(ref cpsr, result);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint RORNoFlags(uint op1, byte offset)
    {
        if (offset > 32) offset = (byte)(offset % 32);

        return offset switch
        {
            0 => op1,
            _ when offset < 32 => ((op1) >> (offset)) | ((op1) << (32 - offset)),
            32 => op1,
            _ => throw new Exception("Invalid value for ROR offset"),
        };
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint MUL(uint op1, uint op2, ref CPSR cpsr)
    {
        var result = op1 * op2;
        SetZeroSignFlags(ref cpsr, result);
        cpsr.CarryFlag = false; // ARMv4 used in GBA so destroy carry flag

        return result;
    }
}
