﻿using GameboyAdvanced.Core.Bus;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Interrupts;

namespace GameboyAdvanced.Core.Dma;

internal class DmaController
{
    private readonly MemoryBus _bus;
    private readonly BaseDebugger _debugger;
    private readonly DmaDataUnit _dmaDataUnit;
    private readonly InterruptInterconnect _interruptInterconnect;
    private readonly Ppu.Ppu _ppu;

    /// <summary>
    /// Like the CPU, when DMA accesses memory addresses it can stretch out N/S
    /// cycles causing what's known as wait states.
    /// 
    /// These wait states block CPU/DMA from executing.
    /// </summary>
    private int _waitStates = 0;

    internal DmaController(MemoryBus bus, BaseDebugger debugger, DmaDataUnit dmaDataUnit, InterruptInterconnect interruptInterconnect, Ppu.Ppu ppu)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _dmaDataUnit = dmaDataUnit ?? throw new ArgumentNullException(nameof(dmaDataUnit));
        _interruptInterconnect = interruptInterconnect ?? throw new ArgumentNullException(nameof(interruptInterconnect));
        _ppu = ppu ?? throw new ArgumentNullException(nameof(ppu));
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
                        break;
                    case StartTiming.VBlank:
                        if (!_ppu.CanVBlankDma())
                        {
                            continue;
                        }
                        break;
                    case StartTiming.HBlank:
                        if (!_ppu.CanHBlankDma())
                        {
                            continue;
                        }
                        break;
                    case StartTiming.Special:
                        throw new NotImplementedException("Special DMA not implemented");
                    default:
                        throw new Exception($"Invalid DMA start timing {_dmaDataUnit.Channels[ii].ControlReg.StartTiming}");
                }
                if (_dmaDataUnit.Channels[ii].ControlReg.StartTiming != StartTiming.Immediate)
                {
                    continue; // TODO - Implement non-immediate mode DMA
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
                // TODO - Pretending that all are S cycles at the moment
                if (_dmaDataUnit.Channels[ii].IntCachedValue.HasValue)
                {
                    _waitStates += (_dmaDataUnit.Channels[ii].ControlReg.Is32Bit)
                        ? _bus.WriteWord(_dmaDataUnit.Channels[ii].IntDestinationAddress, _dmaDataUnit.Channels[ii].IntCachedValue!.Value, 1)
                        : _bus.WriteHalfWord(_dmaDataUnit.Channels[ii].IntDestinationAddress, (ushort)_dmaDataUnit.Channels[ii].IntCachedValue!.Value, 1);
                    _dmaDataUnit.Channels[ii].IntDestinationAddress = (uint)(_dmaDataUnit.Channels[ii].IntDestinationAddress + _dmaDataUnit.Channels[ii].IntDestAddressIncrement); // TODO - Suspect I should be wrapping and masking this address
                    _dmaDataUnit.Channels[ii].IntWordCount--;
                    _dmaDataUnit.Channels[ii].IntCachedValue = null;

                    if (_dmaDataUnit.Channels[ii].IntWordCount == 0)
                    {
                        Console.WriteLine($"DMA{ii} ({_dmaDataUnit.Channels[ii].ControlReg.Is32Bit}) {_dmaDataUnit.Channels[ii].SourceAddress:X8} -> {_dmaDataUnit.Channels[ii].DestinationAddress:X8} - {_dmaDataUnit.Channels[ii].WordCount:X8} complete");
                        _dmaDataUnit.Channels[ii].ControlReg.DmaEnable = false;

                        if (_dmaDataUnit.Channels[ii].ControlReg.IrqOnEnd)
                        {
                            _interruptInterconnect.RaiseInterrupt(ii switch
                            {
                                0 => Interrupt.DMA0,
                                1 => Interrupt.DMA1,
                                2 => Interrupt.DMA2,
                                3 => Interrupt.DMA3,
                                _ => throw new Exception($"Invalid state, no DMA channel with index {ii}")
                            });
                        }
                    }
                }
                else
                {
                    (_dmaDataUnit.Channels[ii].IntCachedValue, var waitStates) = (_dmaDataUnit.Channels[ii].ControlReg.Is32Bit)
                        ? _bus.ReadWord(_dmaDataUnit.Channels[ii].IntSourceAddress, 1)
                        : _bus.ReadHalfWord(_dmaDataUnit.Channels[ii].IntSourceAddress, 1);
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
