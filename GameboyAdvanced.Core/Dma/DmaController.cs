using GameboyAdvanced.Core.Bus;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Interrupts;

namespace GameboyAdvanced.Core.Dma;

public class DmaController
{
    private readonly MemoryBus _bus;
    private readonly BaseDebugger _debugger;
    private readonly DmaDataUnit _dmaDataUnit;
    private readonly InterruptInterconnect _interruptInterconnect;
    private readonly Ppu.Ppu _ppu;
    private readonly Core _cpu;

    internal DmaController(MemoryBus bus, BaseDebugger debugger, DmaDataUnit dmaDataUnit, InterruptInterconnect interruptInterconnect, Ppu.Ppu ppu, Core cpu)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _dmaDataUnit = dmaDataUnit ?? throw new ArgumentNullException(nameof(dmaDataUnit));
        _interruptInterconnect = interruptInterconnect ?? throw new ArgumentNullException(nameof(interruptInterconnect));
        _ppu = ppu ?? throw new ArgumentNullException(nameof(ppu));
        _cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
    }

    internal void Reset()
    {
        
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
        var result = false;

        for (var ii = 0; ii < _dmaDataUnit.Channels.Length; ii++)
        {
            if (_dmaDataUnit.Channels[ii].ControlReg.DmaEnable && _dmaDataUnit.Channels[ii].IsRunning)
            {
                var lowerPriorityActive = false;
                var lowerPriorityMidWriteCycle = false;
                for (var jj = ii + 1; jj < _dmaDataUnit.Channels.Length; jj++)
                {
                    if (_dmaDataUnit.Channels[jj].ControlReg.DmaEnable)
                    {
                        // Wait for any lower priority DMA channels to finish writes before interrupting them
                        lowerPriorityActive = true;

                        if (_dmaDataUnit.Channels[jj].IntCachedValue.HasValue)
                        {
                            lowerPriorityMidWriteCycle = true;
                        }
                    }
                }

                // DMA takes 2 cycles to start and then 1 cycle before writing starts
                if (_dmaDataUnit.Channels[ii].ClocksToStart > 0)
                {
                    _dmaDataUnit.Channels[ii].ClocksToStart--;

                    if (_dmaDataUnit.Channels[ii].ClocksToStart == 0)
                    {
                        result = true;
                    }
                    
                    if (lowerPriorityActive)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                // Oh my goodness this is horrible
                if (lowerPriorityMidWriteCycle) continue;

                // DMA takes 2S cycles per read (apart from the first which is a pair of N cycles)
                if (_dmaDataUnit.Channels[ii].IntCachedValue.HasValue)
                {
                    if (_dmaDataUnit.Channels[ii].ControlReg.Is32Bit)
                    {
                        _bus.WriteWord(_dmaDataUnit.Channels[ii].IntDestinationAddress, _dmaDataUnit.Channels[ii].IntCachedValue!.Value, _dmaDataUnit.Channels[ii].IntDestSeqAccess, 0x4000);
                    }
                    else
                    {
                        _bus.WriteHalfWord(_dmaDataUnit.Channels[ii].IntDestinationAddress, (ushort)_dmaDataUnit.Channels[ii].IntCachedValue!.Value, _dmaDataUnit.Channels[ii].IntDestSeqAccess, 0x4000);
                    }

                    _dmaDataUnit.Channels[ii].IntDestinationAddress = (uint)(_dmaDataUnit.Channels[ii].IntDestinationAddress + _dmaDataUnit.Channels[ii].IntDestAddressIncrement); // TODO - Suspect I should be wrapping and masking this address
                    _dmaDataUnit.Channels[ii].IntWordCount--;
                    _dmaDataUnit.Channels[ii].IntCachedValue = null;
                    if (_dmaDataUnit.Channels[ii].IntDestinationAddress >= 0x0800_0000)
                    {
                        _dmaDataUnit.Channels[ii].IntDestSeqAccess = 1;
                        _dmaDataUnit.Channels[ii].IntSrcSeqAccess = 1;
                    }

                    if (_dmaDataUnit.Channels[ii].IntWordCount == 0)
                    {
                        // One additional cycle after DMA complete
                        //_bus.WaitStates++;
                        _dmaDataUnit.Channels[ii].StopChannel(_interruptInterconnect);
                    }
                }
                else
                {
                    // After masking the internal source address if it falls
                    // inside the BIOS region then we don't read anything and
                    // instead rely on the dmas internal latch register
                    if (_dmaDataUnit.Channels[ii].IntSourceAddress >= 0x0200_0000)
                    {
                        if (_dmaDataUnit.Channels[ii].ControlReg.Is32Bit)
                        {
                            _dmaDataUnit.Channels[ii].InternalLatch = _bus.ReadWord(
                                _dmaDataUnit.Channels[ii].IntSourceAddress, 
                                _dmaDataUnit.Channels[ii].IntSrcSeqAccess, 
                                _cpu.R[15], 
                                _dmaDataUnit.Channels[ii].InternalLatch, 
                                _cpu.Cycles);
                        }
                        else
                        {
                            _dmaDataUnit.Channels[ii].InternalLatch = _bus.ReadHalfWord(
                                _dmaDataUnit.Channels[ii].IntSourceAddress, 
                                _dmaDataUnit.Channels[ii].IntSrcSeqAccess, 
                                _cpu.R[15], 
                                _dmaDataUnit.Channels[ii].InternalLatch, 
                                _cpu.Cycles);
                            _dmaDataUnit.Channels[ii].InternalLatch |= (_dmaDataUnit.Channels[ii].InternalLatch << 16);
                        }
                        _dmaDataUnit.Channels[ii].IntCachedValue = _dmaDataUnit.Channels[ii].InternalLatch;
                    }
                    else
                    {
                        if ((_dmaDataUnit.Channels[ii].IntDestinationAddress & 0b10) != 0)
                        {
                            _dmaDataUnit.Channels[ii].IntCachedValue = _dmaDataUnit.Channels[ii].InternalLatch >> 16;
                        }
                        else
                        {
                            _dmaDataUnit.Channels[ii].IntCachedValue = _dmaDataUnit.Channels[ii].InternalLatch;
                        }
                    }
                    
                    _dmaDataUnit.Channels[ii].IntSourceAddress = (uint)(_dmaDataUnit.Channels[ii].IntSourceAddress + _dmaDataUnit.Channels[ii].IntSrcAddressIncrement); // TODO - Suspect I should be wrapping and masking this address
                    if (_dmaDataUnit.Channels[ii].IntSourceAddress >= 0x0800_0000)
                    {
                        _dmaDataUnit.Channels[ii].IntDestSeqAccess = 1;
                        _dmaDataUnit.Channels[ii].IntSrcSeqAccess = 1;
                    }
                }

                // Only one DMA runs at a time in priority order from 0-3, return true here if a DMA ran
                return true;
            }
        }

        return result;
    }
}
