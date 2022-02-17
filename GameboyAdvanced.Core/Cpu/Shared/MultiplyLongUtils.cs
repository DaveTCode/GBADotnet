namespace GameboyAdvanced.Core.Cpu.Shared;

/// <summary>
/// Multiply long is obviously very similar to multiply but is split into its
/// own utils class here because the way that register writeback works is
/// sufficiently different as to get clearer code with duplication than 
/// branching.
/// </summary>
internal static unsafe class MultiplyLongUtils
{
    private static int _requiredCycles;
    private static int _currentCycles;
    private static uint _destinationRegHi;
    private static uint _destinationRegLo;
    private static ulong _multiplyResult;

    internal static void SetupForSignedMultiplyAccumulateLongFlags(Core core, uint rdHi, uint rdLo, uint rs, uint rm)
    {
        SetupForMultiplyLongCommon(core, rdHi, rdLo);
        var longAccumulator = (long)(((ulong)core.R[rdHi] << 32) | core.R[rdLo]);
        _requiredCycles = MultiplyUtils.CyclesForMultiplyA(core.R[rs]) + 2;
        _multiplyResult = (ulong)(((long)(int)core.R[rs] * (long)(int)core.R[rm]) + longAccumulator);
        core.NextExecuteAction = &MultiplyCycleWFlags;
    }

    internal static void SetupForSignedMultiplyLongFlags(Core core, uint rdHi, uint rdLo, uint rs, uint rm)
    {
        SetupForMultiplyLongCommon(core, rdHi, rdLo);
        _requiredCycles = MultiplyUtils.CyclesForMultiplyA(core.R[rs]) + 1;
        _multiplyResult = (ulong)((long)(int)core.R[rs] * (long)(int)core.R[rm]);
        core.NextExecuteAction = &MultiplyCycleWFlags;
    }

    internal static void SetupForSignedMultiplyAccumulateLong(Core core, uint rdHi, uint rdLo, uint rs, uint rm)
    {
        SetupForMultiplyLongCommon(core, rdHi, rdLo);
        var longAccumulator = (long)(((ulong)core.R[rdHi] << 32) | core.R[rdLo]);
        _requiredCycles = MultiplyUtils.CyclesForMultiplyA(core.R[rs]) + 2;
        _multiplyResult = (ulong)(((long)(int)core.R[rs] * (long)(int)core.R[rm]) + longAccumulator);
        core.NextExecuteAction = &MultiplyCycle;
    }

    internal static void SetupForSignedMultiplyLong(Core core, uint rdHi, uint rdLo, uint rs, uint rm)
    {
        SetupForMultiplyLongCommon(core, rdHi, rdLo);
        _requiredCycles = MultiplyUtils.CyclesForMultiplyA(core.R[rs]) + 1;
        _multiplyResult = (ulong)((long)core.R[rs] * core.R[rm]);
        core.NextExecuteAction = &MultiplyCycle;
    }

    internal static void SetupForMultiplyLongAccumulateFlags(Core core, uint rdHi, uint rdLo, uint rs, uint rm)
    {
        SetupForMultiplyLongCommon(core, rdHi, rdLo);
        var longAccumulator = ((ulong)core.R[rdHi] << 32) | core.R[rdLo];
        _multiplyResult = (core.R[rs] * (ulong)core.R[rm]) + longAccumulator;
        _requiredCycles = MultiplyUtils.CyclesForMultiplyB(core.R[rs]) + 2;
        core.NextExecuteAction = &MultiplyCycleWFlags;
    }

    internal static void SetupForMultiplyLongFlags(Core core, uint rdHi, uint rdLo, uint rs, uint rm)
    {
        SetupForMultiplyLongCommon(core, rdHi, rdLo);
        _requiredCycles = MultiplyUtils.CyclesForMultiplyB(core.R[rs]) + 1;
        _multiplyResult = core.R[rs] * (ulong)core.R[rm];
        core.NextExecuteAction = &MultiplyCycleWFlags;
    }

    internal static void SetupForMultiplyLongAccumulate(Core core, uint rdHi, uint rdLo, uint rs, uint rm)
    {
        SetupForMultiplyLongCommon(core, rdHi, rdLo);
        var longAccumulator = ((ulong)core.R[rdHi] << 32) | core.R[rdLo];
        _multiplyResult = (core.R[rs] * (ulong)core.R[rm]) + longAccumulator;
        _requiredCycles = MultiplyUtils.CyclesForMultiplyB(core.R[rs]) + 2;
        core.NextExecuteAction = &MultiplyCycle;
    }

    internal static void SetupForMultiplyLong(Core core, uint rdHi, uint rdLo, uint rs, uint rm)
    {
        SetupForMultiplyLongCommon(core, rdHi, rdLo);
        _requiredCycles = MultiplyUtils.CyclesForMultiplyB(core.R[rs]) + 1;
        _multiplyResult = core.R[rs] * (ulong)core.R[rm];
        core.NextExecuteAction = &MultiplyCycle;
    }

    private static void SetupForMultiplyLongCommon(Core core, uint rdHi, uint rdLo)
    {
        core.SEQ = false;
        core.nOPC = true;
        core.nMREQ = true;
        _destinationRegHi = rdHi;
        _destinationRegLo = rdLo;
        _currentCycles = 0;
    }

    internal static void MultiplyCycle(Core core, uint instruction)
    {
        if (_currentCycles == _requiredCycles)
        {
            core.R[_destinationRegHi] = (uint)(_multiplyResult >> 32);
            core.R[_destinationRegLo] = (uint)_multiplyResult;
            Core.ResetMemoryUnitForOpcodeFetch(core, instruction);
        }
        else
        {
            _currentCycles++;
        }
    }

    internal static void MultiplyCycleWFlags(Core core, uint instruction)
    {
        if (_currentCycles == _requiredCycles)
        {
            core.R[_destinationRegHi] = (uint)(_multiplyResult >> 32);
            core.R[_destinationRegLo] = (uint)_multiplyResult;
            ALU.SetZeroSignFlags(ref core.Cpsr, _multiplyResult);
            // TODO - The carry/overflow flags are set to a meaningless value. Ok, but what.
            Core.ResetMemoryUnitForOpcodeFetch(core, instruction);
        }
        else
        {
            _currentCycles++;
        }
    }
}
