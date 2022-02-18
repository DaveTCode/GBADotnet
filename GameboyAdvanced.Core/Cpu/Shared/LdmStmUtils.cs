namespace GameboyAdvanced.Core.Cpu.Shared;

/// <summary>
/// LDM/STM and the PUSH/POP variants on the Thumb instruction set have a
/// _lot_ in common so to avoid duplicating rather complicated code this
/// class exists to hold the common shared state/functions.
/// </summary>
internal static class LdmStmUtils
{
    /// <summary>
    /// Whilst a STM/LDM operation is going on we need to track which register
    /// is going to be stored/loaded on the next cycle.
    /// 
    /// In other words, this is nasty global state about how far through a 
    /// stm/ldm instruction we've got.
    /// </summary>
    internal static uint[] _storeLoadMultipleState = new uint[16];
    internal static int _storeLoadMultiplePopCount;
    internal static int _storeLoadMultiplePtr;
    internal static uint _storeLoadMutipleFinalWritebackValue;
    internal static bool _storeLoadMultipleDoWriteback;
    internal static uint _cachedLdmValue;
    internal static int _writebackRegister;
    internal static bool _useBank0Regs;

    internal static void Reset()
    {
        _storeLoadMultiplePopCount = 0;
        _storeLoadMultiplePtr = 0;
        _useBank0Regs = false;
    }

    /// <summary>
    /// Store multiple registers is a lot simpler than load multiple registers 
    /// in cycle timing.
    /// 
    /// The first cycle sets up A & D along with configuring the memory unit 
    /// for a write cycle. Each following cycle assumes that write has happened
    /// and simply sets A & D again.
    /// 
    /// After all writes have happened (the same cycle that the last write has
    /// happened on the memory unit) the writeback happens and the memory unit
    /// is reset for opcode fetches.
    /// </summary>
    internal static void stm_registerWriteCycle(Core core, uint _)
    {
        if (_storeLoadMultiplePtr >= _storeLoadMultiplePopCount)
        {
            if (_storeLoadMultipleDoWriteback)
            {
                if (_useBank0Regs)
                {
                    core.WriteUserModeRegister(_writebackRegister, _storeLoadMutipleFinalWritebackValue);
                }
                else
                {
                    core.R[_writebackRegister] = _storeLoadMutipleFinalWritebackValue;
                }
                
                if (_writebackRegister == 15) 
                {
                    core.ClearPipeline(); // TODO - This is a really bad idea, why would you use PC as writeback, probably needs a special log
                }
            }

            Core.ResetMemoryUnitForOpcodeFetch(core, _);
        }
        else
        {
            core.A += 4;
            core.D = _useBank0Regs
                ? core.GetUserModeRegister((int)_storeLoadMultipleState[_storeLoadMultiplePtr])
                : core.R[_storeLoadMultipleState[_storeLoadMultiplePtr]];

            _storeLoadMultiplePtr++;
        }
    }

    /// <summary>
    /// LDM* functions take several cycles depending on the number of memory 
    /// reads required.
    /// The first cycle (handled by ldm*_* partials) calculates the first 
    /// address and stores it on A along with setting the memory unit up for
    /// non-opcode reads.
    /// 
    /// The second cycle expects that the memory unit will have loaded that 
    /// address value into core.D but doesn't do _anything with it_ (here we 
    /// have to cache it off).
    /// 
    /// The third cycle (and all following) load the value we cached into the 
    /// relevant register.
    /// 
    /// So an LDM which reads R0 and R1 will take 4 cycles
    /// - Set up A (ldm* partial function - not this function)
    /// - Start reading (non-SEQ) - cache off core.D into local variable and 
    ///   increment A for next read
    /// - Set R0 with value read, cache core.D into local variable and since 
    ///   this the last value has been read drive nMREQ high so that the next 
    ///   cycle is internal with no memory accesses
    /// - Set R1 to cached value, set writeback value, reset memory unit for 
    ///   opcode fetch.
    /// </summary>
    internal static void ldm_registerReadCycle(Core core, uint _)
    {
        if (_storeLoadMultiplePtr > 0)
        {
            if (_useBank0Regs)
            {
                core.WriteUserModeRegister((int)_storeLoadMultipleState[_storeLoadMultiplePtr - 1], _cachedLdmValue);
            }
            else
            {
                core.R[_storeLoadMultipleState[_storeLoadMultiplePtr - 1]] = _cachedLdmValue;
            }
            
            if (_storeLoadMultipleState[_storeLoadMultiplePtr - 1] == 15)
            {
                core.ClearPipeline();
            }
        }

        _cachedLdmValue = core.D;
        core.A += 4;

        if (_storeLoadMultiplePtr == _storeLoadMultiplePopCount - 1)
        {
            core.A = core.R[15];
            core.SEQ = true;
            core.MAS = core.Cpsr.ThumbMode ? BusWidth.HalfWord : BusWidth.Word;
            core.nMREQ = true;
        }

        if (_storeLoadMultiplePtr == _storeLoadMultiplePopCount)
        {
            if (_storeLoadMultipleDoWriteback)
            {
                if (_useBank0Regs)
                {
                    core.WriteUserModeRegister(_writebackRegister, _storeLoadMutipleFinalWritebackValue);
                }
                else
                {
                    core.R[_writebackRegister] = _storeLoadMutipleFinalWritebackValue;
                }
                
                if (_writebackRegister == 15)
                {
                    core.ClearPipeline();
                }
            }

            Core.ResetMemoryUnitForOpcodeFetch(core, _);
        }

        _storeLoadMultiplePtr++;
    }
}
