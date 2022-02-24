using static GameboyAdvanced.Core.Cpu.ALU;
using System.Runtime.CompilerServices;

namespace GameboyAdvanced.Core.Cpu;

internal static class Shifter
{
    /// <summary>
    /// LSL from the barrel shifter is identical whether called with an 
    /// immediate or register operand.
    /// </summary>
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
    internal static uint LSRRegister(uint op1, byte offset, ref CPSR cpsr)
    {
        var (result, carry) = offset switch
        {
            0 => (op1, cpsr.CarryFlag),
            32 => (0u, (op1 & 0x8000_0000) == 0x8000_0000),
            _ when offset < 32 => (op1 >> offset, ((op1 >> (offset - 1)) & 1) == 1),
            _ => (0u, false),
        };

        cpsr.CarryFlag = carry;
        SetZeroSignFlags(ref cpsr, result);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint LSRImmediate(uint op1, byte offset, ref CPSR cpsr)
    {
        var (result, carry) = offset switch
        {
            0 => (0u, (op1 & 0x8000_0000) == 0x8000_0000),
            _ => (op1 >> offset, ((op1 >> (offset - 1)) & 1) == 1),
        };

        cpsr.CarryFlag = carry;
        SetZeroSignFlags(ref cpsr, result);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint LSRRegisterNoFlags(uint op1, byte offset) => offset switch
    {
        0 => op1,
        32 => 0u,
        _ when offset < 32 => op1 >> offset,
        _ => 0u,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint LSRImmediateNoFlags(uint op1, byte offset) => offset switch
    {
        0 => 0u,
        _ => op1 >> offset,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ASRRegister(uint op1, byte offset, ref CPSR cpsr)
    {
        var (result, carry) = offset switch
        {
            0 => (op1, cpsr.CarryFlag),
            _ when offset < 32 => ((uint)((int)op1 >> offset), ((op1 >> (offset - 1)) & 1) == 1),
            _ => ((op1 & 0x8000_0000) == 0x8000_0000 ? 0xFFFF_FFFF : 0, (op1 & 0x8000_0000) == 0x8000_0000),
        };

        cpsr.CarryFlag = carry;
        SetZeroSignFlags(ref cpsr, result);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ASRImmediate(uint op1, byte offset, ref CPSR cpsr)
    {
        var (result, carry) = offset switch
        {
            0 => ((op1 & 0x8000_0000) == 0 ? 0x0000_0000 : 0xFFFF_FFFF, (op1 & 0x8000_0000) == 0x8000_0000),
            _ => ((uint)((int)op1 >> offset), ((op1 >> (offset - 1)) & 1) == 1),
        };

        cpsr.CarryFlag = carry;
        SetZeroSignFlags(ref cpsr, result);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ASRRegisterNoFlags(uint op1, byte offset) => offset switch
    {
        0 => op1,
        _ when offset < 32 => (uint)((int)op1 >> offset),
        _ => (op1 & 0x8000_0000) == 0x8000_0000 ? 0xFFFF_FFFF : 0,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ASRImmediateNoFlags(uint op1, byte offset) => offset switch
    {
        0 => (op1 & 0x8000_0000) == 0 ? 0x0000_0000 : 0xFFFF_FFFF,
        _ => (uint)((int)op1 >> offset),
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ROR(uint op1, byte offset, ref CPSR cpsr)
    {
        if (offset > 32) offset = (byte)(offset & 31);
        if (offset == 0)
        {
            SetZeroSignFlags(ref cpsr, op1);
            return op1;
        }

        var (result, carry) = offset switch
        {
            _ when offset < 32 =>
            (
                RORInternal(op1, offset),
                ((op1 >> (offset - 1)) & 1) == 1
            ),
            32 => (op1, (op1 & 0x8000_0000) == 0x8000_0000),
            _ => throw new Exception("Invalid value for ROR offset"),
        };

        cpsr.CarryFlag = carry;
        SetZeroSignFlags(ref cpsr, result);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint RORIncRRX(uint op1, byte offset, ref CPSR cpsr)
    {
        // ROR #0 is used to encode RRX instead
        if (offset == 0)
        {
            var c = cpsr.CarryFlag ? 0x8000_0000 : 0;
            cpsr.CarryFlag = (op1 & 0b1) == 0b1;
            SetZeroSignFlags(ref cpsr, op1 >> 1);
            return (op1 >> 1) | c;
        }

        var (result, carry) = offset switch
        {
            _ when offset < 32 =>
            (
                RORInternal(op1, offset),
                ((op1 >> (offset - 1)) & 1) == 1
            ),
            32 => (op1, (op1 & 0x8000_0000) == 0x8000_0000),
            _ => throw new Exception("Invalid value for ROR offset"),
        };

        cpsr.CarryFlag = carry;
        SetZeroSignFlags(ref cpsr, result);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint RORNoFlags(uint op1, byte offset)
    {
        if (offset > 32) offset = (byte)(offset & 31);

        return offset switch
        {
            0 => op1,
            _ when offset < 32 => RORInternal(op1, offset),
            32 => op1,
            _ => throw new Exception("Invalid value for ROR offset"),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint RORNoFlagsIncRRX(uint op1, byte offset, ref CPSR cpsr)
    {
        // ROR #0 is used to encode RRX instead
        if (offset == 0)
        {
            var c = cpsr.CarryFlag ? 0x8000_0000 : 0;
            return (op1 >> 1) | c;
        }
        if (offset > 32) offset = (byte)(offset & 31);

        return RORInternal(op1, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint RORInternal(uint op1, byte offset) => ((op1) >> (offset)) | ((op1) << (32 - offset));
}
