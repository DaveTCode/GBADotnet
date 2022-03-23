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

    /// <summary>
    /// Like the CPU, when DMA accesses memory addresses it can stretch out N/S
    /// cycles causing what's known as wait states.
    /// 
    /// These wait states block CPU/DMA from executing.
    /// </summary>
    private int _waitStates = 0;

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
                switch (_dmaDataUnit.Channels[ii].ControlReg.StartTiming)
                {
                    case StartTiming.Immediate:
                        _dmaDataUnit.Channels[ii].IsRunning = true;
                        break;
                    case StartTiming.VBlank:
                        if (!_dmaDataUnit.Channels[ii].IsRunning)
                        {
                            // Wait until vblank to start DMA
                            if (!_ppu.CanVBlankDma()) continue;

                            _dmaDataUnit.Channels[ii].IsRunning = true;
                        }
                        break;
                    case StartTiming.HBlank:
                        if (!_dmaDataUnit.Channels[ii].IsRunning)
                        {
                            // Wait until hblank to start DMA
                            if (!_ppu.CanHBlankDma()) continue;

                            _dmaDataUnit.Channels[ii].IsRunning = true;
                        }
                        break;
                    case StartTiming.Special:
                        continue; // TODO - Implement special DMA
                    default:
                        throw new Exception($"Invalid DMA start timing {_dmaDataUnit.Channels[ii].ControlReg.StartTiming}");
                }

                // DMA takes 2 cycles to start and then 1 cycle before writing starts
                // TODO - Lots about this I'm not sure about, No$ docs say 4I cycles if both src/dest in gamepak but I bet that's not at the start
                // TODO - Do these cycles cause paused CPU? Right now I say no. Do they count down on all channels at the same time? No for now.
                if (_dmaDataUnit.Channels[ii].ClocksToStart > 0)
                {
                    _dmaDataUnit.Channels[ii].ClocksToStart--;
                    return _dmaDataUnit.Channels[ii].ClocksToStart == 0;
                }

                // DMA takes 2S cycles per read (apart from the first which is a pair of N cycles)
                if (_dmaDataUnit.Channels[ii].IntCachedValue.HasValue)
                {
                    _waitStates += (_dmaDataUnit.Channels[ii].ControlReg.Is32Bit)
                        ? _bus.WriteWord(_dmaDataUnit.Channels[ii].IntDestinationAddress, _dmaDataUnit.Channels[ii].IntCachedValue!.Value, _dmaDataUnit.Channels[ii].IntDestSeqAccess)
                        : _bus.WriteHalfWord(_dmaDataUnit.Channels[ii].IntDestinationAddress, (ushort)_dmaDataUnit.Channels[ii].IntCachedValue!.Value, _dmaDataUnit.Channels[ii].IntDestSeqAccess);
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
                        _waitStates++;
                        _dmaDataUnit.Channels[ii].StopChannel(_interruptInterconnect);
                    }
                }
                else
                {
                    // After masking the internal source address if it falls
                    // inside the BIOS region then we don't read anything and
                    // instead rely on the dmas internal latch register
                    var waitStates = 0;
                    if (_dmaDataUnit.Channels[ii].IntSourceAddress >= 0x0200_0000)
                    {
                        if (_dmaDataUnit.Channels[ii].ControlReg.Is32Bit)
                        {
                            _dmaDataUnit.Channels[ii].InternalLatch = _bus.ReadWord(
                                _dmaDataUnit.Channels[ii].IntSourceAddress, 
                                _dmaDataUnit.Channels[ii].IntSrcSeqAccess, 
                                _cpu.R[15], 
                                _dmaDataUnit.Channels[ii].InternalLatch, 
                                _cpu.Cycles, 
                                ref waitStates);
                        }
                        else
                        {
                            _dmaDataUnit.Channels[ii].InternalLatch = _bus.ReadHalfWord(
                                _dmaDataUnit.Channels[ii].IntSourceAddress, 
                                _dmaDataUnit.Channels[ii].IntSrcSeqAccess, 
                                _cpu.R[15], 
                                _dmaDataUnit.Channels[ii].InternalLatch, 
                                _cpu.Cycles, 
                                ref waitStates);
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
                    
                    _waitStates += waitStates;
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

        return false;
    }
}
