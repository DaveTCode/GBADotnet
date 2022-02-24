using GameboyAdvanced.Core.Bus;
using GameboyAdvanced.Core.Debug;

namespace GameboyAdvanced.Core.Dma;

internal class DmaController
{
    private readonly MemoryBus _bus;
    private readonly BaseDebugger _debugger;
    private readonly DmaDataUnit _dmaDataUnit;

    /// <summary>
    /// Like the CPU, when DMA accesses memory addresses it can stretch out N/S
    /// cycles causing what's known as wait states.
    /// 
    /// These wait states block CPU/DMA from executing.
    /// </summary>
    private int _waitStates = 0;

    internal DmaController(MemoryBus bus, BaseDebugger debugger, DmaDataUnit dmaDataUnit)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _dmaDataUnit = dmaDataUnit ?? throw new ArgumentNullException(nameof(dmaDataUnit));
    }

    internal void Reset()
    {
        _waitStates = 0;
    }

    /// <summary>
    /// Steps the DMA controller and returns a boolean indicating whether DMA 
    /// is active (and therefore the CPU should be paused)
    /// </summary>
    /// 
    /// <remarks>
    /// TODO - Cycle timing of DMA isn't something I properly understand yet.
    /// Likely right with memory read/writes and wait states but not with
    /// where the additional I cycles go.
    /// </remarks>
    /// 
    /// <returns>
    /// true if any DMA channel is active, false otherwise
    /// </returns>
    internal bool Step()
    {
        if (_waitStates > 0)
        {
            _waitStates--;
            return true;
        }

        for (var ii = 0; ii < _dmaDataUnit.Channels.Length; ii++)
        {
            if (_dmaDataUnit.Channels[ii].ControlReg.DmaEnable)
            {
                if (_dmaDataUnit.Channels[ii].ControlReg.StartTiming != StartTiming.Immediate)
                {
                    throw new NotImplementedException("Only immediate DMA implemented at the moment");
                }

                // DMA takes 2 I cycles to start
                // TODO - Lots about this I'm not sure about, No$ docs say 4I cycles if both src/dest in gamepak but I bet that's not at the start
                // TODO - Do these cycles cause paused CPU? Right now I say no. Do they count down on all channels at the same time? No for now.
                if (_dmaDataUnit.Channels[ii].ClocksToStart > 0)
                {
                    _dmaDataUnit.Channels[ii].ClocksToStart--;
                    continue;
                }

                // DMA takes 2S cycles per read (apart from the first which is a pair of N cycles)
                if (_dmaDataUnit.Channels[ii].IntCachedValue.HasValue)
                {
                    _waitStates += (_dmaDataUnit.Channels[ii].ControlReg.Is32Bit)
                        ? _bus.WriteWord(_dmaDataUnit.Channels[ii].IntDestinationAddress, _dmaDataUnit.Channels[ii].IntCachedValue!.Value)
                        : _bus.WriteHalfWord(_dmaDataUnit.Channels[ii].IntDestinationAddress, (ushort)_dmaDataUnit.Channels[ii].IntCachedValue!.Value);
                    _dmaDataUnit.Channels[ii].IntDestinationAddress = (uint)(_dmaDataUnit.Channels[ii].IntDestinationAddress + _dmaDataUnit.Channels[ii].IntDestAddressIncrement); // TODO - Suspect I should be wrapping and masking this address
                    _dmaDataUnit.Channels[ii].IntWordCount--;
                    _dmaDataUnit.Channels[ii].IntCachedValue = null;

                    if (_dmaDataUnit.Channels[ii].IntWordCount == 0)
                    {
                        _dmaDataUnit.Channels[ii].ControlReg.DmaEnable = false;

                        if (_dmaDataUnit.Channels[ii].ControlReg.IrqOnEnd)
                        {
                            throw new NotImplementedException("No IRQ on DMA implemented yet");
                        }
                    }
                }
                else
                {
                    (_dmaDataUnit.Channels[ii].IntCachedValue, var waitStates) = (_dmaDataUnit.Channels[ii].ControlReg.Is32Bit)
                        ? _bus.ReadWord(_dmaDataUnit.Channels[ii].IntSourceAddress)
                        : _bus.ReadHalfWord(_dmaDataUnit.Channels[ii].IntSourceAddress);
                    _waitStates += waitStates;
                    _dmaDataUnit.Channels[ii].IntSourceAddress = (uint)(_dmaDataUnit.Channels[ii].IntSourceAddress + _dmaDataUnit.Channels[ii].IntSrcAddressIncrement); // TODO - Suspect I should be wrapping and masking this address
                }

                // Only one DMA runs at a time in priority order from 0-3, return true here if a DMA ran
                return true;
            }
        }

        return false;
    }
}
