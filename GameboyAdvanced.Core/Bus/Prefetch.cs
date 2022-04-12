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
public class Prefetcher
{
    public bool _active;
    public uint _currentPreFetchBase;
    public long _cycleNextRequestStart;
    public readonly WaitControl _waitControl;
    public readonly GamePak _gamePak;

    public Prefetcher(WaitControl waitControl, GamePak gamePak)
    {
        _waitControl = waitControl;
        _gamePak = gamePak;
        Reset();
    }

    internal void Reset()
    {
        _currentPreFetchBase = 0;
        _cycleNextRequestStart = 0;
        _active = false;
    }

    private void CheckAdjustPrefetchBuffer(uint address, int waitStateIx, int seq, long currentCycles, ref int waitStates)
    {
        // If prefetch is enabled, the memory request is the head of the pre fetch buffer, and the next memory request would have already started
        if (_waitControl.EnableGamepakPrefetch && address == (_currentPreFetchBase + 2) && _cycleNextRequestStart < currentCycles && _active)
        {
            var waitStatesNoPrefetch = _waitControl.WaitStates[waitStateIx][1]; // Always SEQ in prefetch buffer
            var adjustedWaitStates = waitStatesNoPrefetch - (int)(currentCycles - _cycleNextRequestStart);
            _cycleNextRequestStart += waitStatesNoPrefetch + 1;

            if (adjustedWaitStates > 0)
            {
                waitStates += adjustedWaitStates;
            }
        }
        else
        {
            var waitStatesNoPrefetch = _waitControl.WaitStates[waitStateIx][seq];
            waitStates += waitStatesNoPrefetch;
            _cycleNextRequestStart = currentCycles + waitStatesNoPrefetch + 1; // This is the cycle on which the next request to gamepak would begin by prefetch unit
            _active = _waitControl.EnableGamepakPrefetch;
        }

        _currentPreFetchBase = address;
    }

    private void CheckAdjustPrefetchBufferWord(uint address, int waitStateIx, int seq, long currentCycles, ref int waitStates)
    {
        // If prefetch is enabled, the memory request is the head of the pre fetch buffer, and the next memory request would have already started
        if (_waitControl.EnableGamepakPrefetch && address == (_currentPreFetchBase + 4) && _cycleNextRequestStart < currentCycles && _active)
        {
            var waitStatesNoPrefetch = _waitControl.WaitStates[waitStateIx][1] * 2 + 1; // Always SEQ in prefetch buffer
            var adjustedWaitStates = waitStatesNoPrefetch - (int)(currentCycles - _cycleNextRequestStart);
            _cycleNextRequestStart += waitStatesNoPrefetch + 1;

            if (adjustedWaitStates > 0)
            {
                waitStates += adjustedWaitStates;
            }
        }
        else
        {
            var waitStatesNoPrefetch = _waitControl.WaitStates[waitStateIx][seq] + _waitControl.WaitStates[waitStateIx][1] + 1;
            waitStates += waitStatesNoPrefetch;
            _cycleNextRequestStart = currentCycles + waitStatesNoPrefetch + 1; // This is the cycle on which the next request to gamepak would begin by prefetch unit
            _active = _waitControl.EnableGamepakPrefetch;
        }

        _currentPreFetchBase = address;
    }

    internal byte ReadGamePakByte(uint address, int waitStatesIx, int seq, long currentCycles, ref int waitStates)
    {
        CheckAdjustPrefetchBuffer(address, waitStatesIx, seq, currentCycles, ref waitStates);

        return _gamePak.ReadByte(address & 0x1FF_FFFF);
    }

    internal ushort ReadGamePakHalfWord(uint address, int waitStatesIx, int seq, long currentCycles, ref int waitStates)
    {
        CheckAdjustPrefetchBuffer(address, waitStatesIx, seq, currentCycles, ref waitStates);

        return _gamePak.ReadHalfWord(address & 0x1FF_FFFF);
    }

    internal uint ReadGamePakWord(uint address, int waitStatesIx, int seq, long currentCycles, ref int waitStates)
    {
        CheckAdjustPrefetchBufferWord(address, waitStatesIx, seq, currentCycles, ref waitStates);

        return _gamePak.ReadWord(address & 0x1FF_FFFF);
    }

    public override string ToString()
    {
        return $"Active={_active}, _currentPreFetchBase={_currentPreFetchBase}, _cycleNextRequestStart={_cycleNextRequestStart}";
    }
}
