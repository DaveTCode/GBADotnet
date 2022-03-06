using System.Runtime.CompilerServices;

namespace GameboyAdvanced.Core.Cpu.Shared;

/// <summary>
/// LDR/STR & LDRH/STRH can roughly be treated in common across both Thumb and
/// ARM instruction sets. This static class groups together the common logic 
/// required.
/// </summary>
internal static unsafe class LdrStrUtils
{
    private static int _ldrReg;
    private static int _writebackReg;
    private static uint _writebackVal;
    private static bool _doWriteback;
    private static delegate*<uint, uint, uint> _ldrCastFunc;
    private static uint _cachedValue;

    internal static uint LDRW(uint addressBus, uint dataBus)
    {
        var rotate = 8 * (int)(addressBus & 0b11);
        return (dataBus >> rotate) | (dataBus << (32 - rotate));
    }
    internal static uint LDRHW(uint addressBus, uint dataBus)
    {
        var rotate = 8 * (int)(addressBus & 0b1);
        return (dataBus >> rotate) | (dataBus << (32 - rotate));
    }
    internal static uint LDRSHW(uint addressBus, uint dataBus)
    {
        if ((addressBus & 1) == 1)
        {
            return LDRSB(addressBus, dataBus >> 8);
        }

        // TODO - Is there a more efficient way to sign extend in C# which doesn't branch?
        var bit15 = (dataBus >> 15) & 0b1;
        return bit15 == 1
            ? 0xFFFF_0000 | (ushort)(short)dataBus
            : (ushort)(short)dataBus;
    }

    internal static uint LDRB(uint addressBus, uint dataBus) => (byte)dataBus;
    internal static uint LDRSB(uint addressBus, uint dataBus)
    {
        // "and set bits 8 - 31 of Rd to bit 7"
        // TODO - Is there a more efficient way to sign extend in C# which doesn't branch?
        var bit7 = (dataBus >> 7) & 0b1;
        return bit7 == 1
            ? 0xFFFF_FF00 | (ushort)(sbyte)dataBus
            : (ushort)(sbyte)dataBus;
    }

    /// <summary>
    /// LDR takes 3 cycles (1N + 1S + 1I).
    /// 
    /// The first cycle (e.g. Thumb + LDR_PC_Offset) calculates the address 
    /// and puts it on the address bus.
    /// 
    /// The second cycle (this one) is a noop from the point of view of the executing 
    /// pipeline (but is when the memory unit will retrieve A into D)
    /// 
    /// The third cycle is <see cref="LDRCycle3(Core, uint)"/>
    /// </summary>
    internal static void LDRCycle2(Core core, uint instruction)
    {
        _cachedValue = _ldrCastFunc(core.A, core.D);
        // After an LDR the address bus (A) shows current PC + 2n and it's set up
        // for opcode fetch but nMREQ is driven high for one internal cycle
        core.A = core.R[15];
        core.nOPC = false;
        core.nRW = false;
        core.SEQ = 0;
        core.nMREQ = true;
        core.MAS = core.Cpsr.ThumbMode ? BusWidth.HalfWord : BusWidth.Word;

        if (_doWriteback)
        {
            core.R[_writebackReg] = _writebackVal;
            if (_writebackReg == 15)
            {
                // TODO - This would be really naughty - probably want an event to hook into the debug system
                core.ClearPipeline();
            }
        }

        // "This third cycle can normally be merged with 
        // the next prefetch cycle to form one memory N - cycle"
        // -
        // TODO, what does that mean here, does nMREQ actually go low in most
        // cases? How to know?
        core.NextExecuteAction = &LDRCycle3;
    }

    /// <summary>
    /// LDR takes 3 cycles (1N + 1S + 1I). <see cref="LDRCycle2(Core, uint)"/> 
    /// for details of cycle 1 & 2.
    /// 
    /// On cycle 3 the data bus value is written back into the destination 
    /// register and nMREQ is driven low so that the next cycle will cause
    /// an opcode fetch.
    /// </summary>
    internal static void LDRCycle3(Core core, uint instruction)
    {
        core.R[_ldrReg] = _cachedValue;
        
        if (_ldrReg == 15)
        {
            core.ClearPipeline();
        }
        core.nMREQ = false;
        core.AIncrement = (uint)core.MAS;

        core.MoveExecutePipelineToNextInstruction();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LDRCommon(Core core, uint address, BusWidth busWidth, int destinationReg, delegate*<uint, uint, uint> castFunc)
    {
        _doWriteback = false;
        _ldrCastFunc = castFunc;
        _ldrReg = destinationReg;
        core.A = address;
        core.AIncrement = 0;
        core.MAS = busWidth;
        core.nRW = false;
        core.nOPC = true;
        core.SEQ = 0;
        core.NextExecuteAction = &LDRCycle2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LDRCommonWriteback(Core core, uint address, BusWidth busWidth, int destinationReg, delegate*<uint, uint, uint> castFunc, int writebackReg, uint writebackVal)
    {
        LDRCommon(core, address, busWidth, destinationReg, castFunc);
        _doWriteback = true;
        _writebackReg = writebackReg;
        _writebackVal = writebackVal;
    }

    internal static void STRCycle2(Core core, uint instruction)
    {
        if (_doWriteback)
        {
            core.R[_writebackReg] = _writebackVal;
            if (_writebackReg == 15)
            {
                // TODO - This would be really naughty - probably want an event to hook into the debug system
                core.ClearPipeline();
            }
        }
        Core.ResetMemoryUnitForOpcodeFetch(core, instruction);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void STRCommon(Core core, uint address, uint data, BusWidth busWidth)
    {
        _doWriteback = false;
        core.A = address;
        core.AIncrement = 0;
        core.D = data;
        core.MAS = busWidth;
        core.nRW = true;
        core.nOPC = true;
        core.SEQ = 0;
        core.NextExecuteAction = &STRCycle2;
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void STRCommonWriteback(Core core, uint address, uint data, BusWidth busWidth, int writebackReg, uint writebackVal)
    {
        STRCommon(core, address, data, busWidth);
        _doWriteback = true;
        _writebackReg = writebackReg;
        _writebackVal = writebackVal;
    }
}
