namespace GameboyAdvanced.Core.Cpu.Shared;

/// <summary>
/// All the multiply operations across Arm/Thumb have aspects in common which 
/// are stored here.
/// </summary>
internal static unsafe class MultiplyUtils
{
    private static int _requiredCycles;
    private static int _currentCycles;
    private static int _destinationReg;
    private static uint _multiplyResult;

    internal static void SetupForMultiplyAccumulateFlags(Core core, int rd, int rs, int rm, int rn)
    {
        SetupForMultiplyAccumulate(core, rd, rs, rm, rn);
        core.NextExecuteAction = &MultiplyCycleWFlags;
    }

    internal static void SetupForMultiplyFlags(Core core, int rd, int rs, int rm)
    {
        SetupForMultiply(core, rd, rs, rm);
        core.NextExecuteAction = &MultiplyCycleWFlags;
    }

    internal static void SetupForMultiplyAccumulate(Core core, int rd, int rs, int rm, int rn)
    {
        SetupForMultiply(core, rd, rs, rm);
        _multiplyResult += core.R[rn];
        _requiredCycles++; // 1 extra I cycle for MLA operation
    }

    internal static void SetupForMultiply(Core core, int rd, int rs, int rm)
    {
        core.SEQ = false;
        core.nOPC = true;
        core.nMREQ = true;
        _destinationReg = rd;
        _currentCycles = 0;

        if ((core.R[rs] & 0xFFFF_FF00) is 0 or 0xFFFF_FF00)
        {
            _requiredCycles = 1;
        }
        else if ((core.R[rs] & 0xFFFF_0000) is 0 or 0xFFFF_0000)
        {
            _requiredCycles = 2;
        }
        else if ((core.R[rs] & 0xFF00_0000) is 0 or 0xFF00_0000)
        {
            _requiredCycles = 3;
        }
        else
        {
            _requiredCycles = 4;
        }

        _multiplyResult = core.R[rs] * core.R[rm];

        core.NextExecuteAction = &MultiplyCycle;
    }

    internal static void MultiplyCycle(Core core, uint instruction)
    {
        if (_currentCycles == _requiredCycles)
        {
            core.R[_destinationReg] = _multiplyResult;
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
            core.R[_destinationReg] = _multiplyResult;
            ALU.SetZeroSignFlags(ref core.Cpsr, _multiplyResult);
            // TODO - The carry flag is set to a meaningless value. Ok, but what.
            Core.ResetMemoryUnitForOpcodeFetch(core, instruction);
        }
        else
        {
            _currentCycles++;
        }
    }
}
