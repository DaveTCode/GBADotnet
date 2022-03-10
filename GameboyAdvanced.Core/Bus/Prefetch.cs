using GameboyAdvanced.Core.Rom;

namespace GameboyAdvanced.Core.Bus;

/// <summary>
/// The GBA has an independent prefetch unit on the A/D lines from the CPU
/// into the GamePak.
/// 
/// Every clock cycle of the cpu that the memory unit is not using those 
/// lines it will fetch the "next" entry in ROM. 
/// 
/// In practice this means either nMREQ is driven high (no memory access)
/// or A lies in another region. Internally the prefetch unit stores 8 16 bit
/// values in a circular buffer.
/// </summary>
internal class Prefetcher
{
    private uint _currentPreFetchBase;
    private ulong _cycleBurstStart;
    private readonly WaitControl _waitControl;
    private readonly GamePak _gamePak;

    internal Prefetcher(WaitControl waitControl, GamePak gamePak)
    {
        _waitControl = waitControl;
        _gamePak = gamePak;
        Reset();
    }

    internal void Reset()
    {
        _currentPreFetchBase = 0;
        _cycleBurstStart = 0;
    }

    private void CheckAdjustPrefetchBuffer(uint address, int waitStatesNoPrefetch, ulong currentCycles, ref int waitStates)
    {
        // If prefetch is enabled and the memory request is the head of the pre fetch buffer
        if (_waitControl.EnableGamepakPrefetch && address == (_currentPreFetchBase + 2))
        {
            var adjustedWaitStates = waitStatesNoPrefetch - (int)(currentCycles - _cycleBurstStart);

            if (adjustedWaitStates > 0)
            {
                waitStates += adjustedWaitStates;
            }
        }
        else
        {
            waitStates += waitStatesNoPrefetch;
        }

        _currentPreFetchBase = address;
        _cycleBurstStart = currentCycles;
    }

    internal byte ReadGamePakByte(uint address, int waitStatesNoPrefetch, ulong currentCycles, ref int waitStates)
    {
        CheckAdjustPrefetchBuffer(address, waitStatesNoPrefetch, currentCycles, ref waitStates);

        return _gamePak.ReadByte(address & 0x1F_FFFF);
    }

    internal ushort ReadGamePakHalfWord(uint address, int waitStatesNoPrefetch, ulong currentCycles, ref int waitStates)
    {
        CheckAdjustPrefetchBuffer(address, waitStatesNoPrefetch, currentCycles, ref waitStates);

        return _gamePak.ReadHalfWord(address & 0x1F_FFFF);
    }

    internal uint ReadGamePakWord(uint address, int waitStatesNoPrefetch1, int waitStatesNoPrefetch2, ulong currentCycles, ref int waitStates)
    {
        CheckAdjustPrefetchBuffer(address, waitStatesNoPrefetch1, currentCycles, ref waitStates);
        CheckAdjustPrefetchBuffer(address + 2, waitStatesNoPrefetch2, currentCycles, ref waitStates);
        
        return _gamePak.ReadWord(address & 0x1F_FFFF);
    }
}
