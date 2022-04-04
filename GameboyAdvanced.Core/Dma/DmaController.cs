using GameboyAdvanced.Core.Bus;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Interrupts;

namespace GameboyAdvanced.Core.Dma;

public unsafe class DmaController
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

    private static delegate*<DmaController, bool> _stepAction = &ReadCycle;
    private static int _currentTimerIx;
    public static bool WriteCycle(DmaController ctrl)
    {
        var channel = ctrl._dmaDataUnit.Channels[_currentTimerIx];

        if (channel.ControlReg.Is32Bit)
        {
            ctrl._bus.WriteWord(channel.IntDestinationAddress, channel.IntCachedValue!.Value, channel.IntDestSeqAccess, 0x4000);
        }
        else
        {
            ctrl._bus.WriteHalfWord(channel.IntDestinationAddress, (ushort)channel.IntCachedValue!.Value, channel.IntDestSeqAccess, 0x4000);
        }

        channel.IntDestinationAddress = (uint)(channel.IntDestinationAddress + channel.IntDestAddressIncrement); // TODO - Suspect I should be wrapping and masking this address
        channel.IntWordCount--;
        channel.IntCachedValue = null;
        if (channel.IntDestinationAddress >= 0x0800_0000)
        {
            channel.IntDestSeqAccess = 1;
            channel.IntSrcSeqAccess = 1;
        }

        if (channel.IntWordCount == 0)
        {
            // One additional cycle after DMA complete
            //_bus.WaitStates++;
            channel.StopChannel(ctrl._interruptInterconnect);
        }

        _stepAction = &ReadCycle;
        return true;
    }

    public static bool ReadCycle(DmaController ctrl)
    {
        var result = false;

        for (var ii = 0; ii < ctrl._dmaDataUnit.Channels.Length; ii++)
        {
            var channel = ctrl._dmaDataUnit.Channels[ii];
            if (channel.ControlReg.DmaEnable && channel.IsRunning)
            {
                var lowerPriorityActive = false;
                for (var jj = ii + 1; jj < ctrl._dmaDataUnit.Channels.Length; jj++)
                {
                    if (channel.ControlReg.DmaEnable)
                    {
                        // Wait for any lower priority DMA channels to finish writes before interrupting them
                        lowerPriorityActive = true;
                    }
                }

                // DMA takes 2 cycles to start and then 1 cycle before writing starts
                if (channel.ClocksToStart > 0)
                {
                    channel.ClocksToStart--;

                    if (channel.ClocksToStart <= 1)
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
                    
                // After masking the internal source address if it falls
                // inside the BIOS region then we don't read anything and
                // instead rely on the dmas internal latch register
                if (channel.IntSourceAddress >= 0x0200_0000)
                {
                    if (channel.ControlReg.Is32Bit)
                    {
                        channel.InternalLatch = ctrl._bus.ReadWord(
                            channel.IntSourceAddress,
                            channel.IntSrcSeqAccess,
                            ctrl._cpu.R[15],
                            channel.InternalLatch,
                            ctrl._cpu.Cycles);
                    }
                    else
                    {
                        channel.InternalLatch = ctrl._bus.ReadHalfWord(
                            channel.IntSourceAddress,
                            channel.IntSrcSeqAccess,
                            ctrl._cpu.R[15],
                            channel.InternalLatch,
                            ctrl._cpu.Cycles);
                        channel.InternalLatch |= (channel.InternalLatch << 16);
                    }
                    channel.IntCachedValue = channel.InternalLatch;
                }
                else
                {
                    if ((channel.IntDestinationAddress & 0b10) != 0)
                    {
                        channel.IntCachedValue = channel.InternalLatch >> 16;
                    }
                    else
                    {
                        channel.IntCachedValue = channel.InternalLatch;
                    }
                }

                channel.IntSourceAddress = (uint)(channel.IntSourceAddress + channel.IntSrcAddressIncrement); // TODO - Suspect I should be wrapping and masking this address
                if (channel.IntSourceAddress >= 0x0800_0000)
                {
                    channel.IntDestSeqAccess = 1;
                    channel.IntSrcSeqAccess = 1;
                }

                _stepAction = &WriteCycle;
                _currentTimerIx = ii;

                // Only one DMA runs at a time in priority order from 0-3, return true here if a DMA ran
                return true;
            }
        }

        return result;
    }

    /// <summary>
    /// Steps the DMA controller and returns a boolean indicating whether DMA 
    /// is active (and therefore the CPU should be paused)
    /// </summary>
    /// 
    /// <returns>
    /// true if any DMA channel is active, false otherwise
    /// </returns>
    internal bool Step()
    {
        return _stepAction(this);
    }
}
