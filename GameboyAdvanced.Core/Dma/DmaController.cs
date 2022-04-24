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
    private readonly Core _cpu;

    internal DmaController(MemoryBus bus, BaseDebugger debugger, DmaDataUnit dmaDataUnit, InterruptInterconnect interruptInterconnect, Core cpu)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _dmaDataUnit = dmaDataUnit ?? throw new ArgumentNullException(nameof(dmaDataUnit));
        _interruptInterconnect = interruptInterconnect ?? throw new ArgumentNullException(nameof(interruptInterconnect));
        _cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
    }

    internal void Reset()
    {
        
    }

    /// <summary>
    /// Only _some_ parts of DMA can continue whilst the system is blocked on 
    /// wait states, specifically DMA channels can perform their initial latch
    /// cycles.
    /// </summary>
    internal void CheckForInternalCycles()
    {
        for (var ii = 0; ii < 4; ii++)
        {
            var channel = _dmaDataUnit.Channels[ii];
            if (channel.IsRunning && channel.ControlReg.DmaEnable)
            {
                if (channel.ClocksToStart >= 2)
                {
                    channel.ClocksToStart--;
                }
            }
        }
    }

    private static delegate*<DmaController, void> _stepAction = &ReadCycle;
    private static int _currentTimerIx;
    public static void WriteCycle(DmaController ctrl)
    {
        if (ctrl._bus.WaitStates > 0) return;

        var channel = ctrl._dmaDataUnit.Channels[_currentTimerIx];

        if (channel.ControlReg.Is32Bit)
        {
            ctrl._bus.WriteWord(channel.IntDestinationAddress, channel.IntCachedValue!.Value, channel.IntDestSeqAccess, 0x4000);
        }
        else
        {
            ctrl._bus.WriteHalfWord(channel.IntDestinationAddress, (ushort)channel.IntCachedValue!.Value, channel.IntDestSeqAccess, 0x4000);
        }

        if (channel.IntDestinationAddress is >= 0x0800_0000 and < 0x0E00_0000)
        {
            channel.IntDestSeqAccess = 1;
            channel.IntSrcSeqAccess = 1;
        }

        channel.IntDestinationAddress = (uint)(channel.IntDestinationAddress + channel.IntDestAddressIncrement); // TODO - Suspect I should be wrapping and masking this address
        channel.IntWordCount--;
        channel.IntCachedValue = null;

        if (channel.IntWordCount == 0)
        {
            // Not 100% sure on this, I need another cycle _somewhere_ in dma
            // and it must at least be whilst the bus is active but it might be
            // before or after. This wait state is a bit of a hack to force the
            // issue but is likely impossible to notice
            channel.ClocksToStop = 1;
            channel.StopChannel(ctrl._interruptInterconnect);
            ctrl._cpu.SEQ = 0;
        }

        _stepAction = &ReadCycle;
    }

    public static void ReadCycle(DmaController ctrl)
    {
        // Assume no DMA channels are accessing the bus, set this whilst checking channels
        ctrl._bus.InUseByDma = false;

        for (var ii = 0; ii < ctrl._dmaDataUnit.Channels.Length; ii++)
        {
            var channel = ctrl._dmaDataUnit.Channels[ii];
            if (channel.ClocksToStop == 1)
            {
                channel.ClocksToStop = 0;
                ctrl._bus.InUseByDma = true;
            }

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

                // DMA takes 3 cycles in total before the first read occurs,
                // 1 to latch internal registers and then 2 whilst it acquires the bus.
                if (channel.ClocksToStart > 0)
                {
                    channel.ClocksToStart--;

                    if (channel.ClocksToStart == 0)
                    {
                        ctrl._bus.InUseByDma = true;
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
                            ctrl._cpu.Cycles,
                            false);
                    }
                    else
                    {
                        channel.InternalLatch = ctrl._bus.ReadHalfWord(
                            channel.IntSourceAddress,
                            channel.IntSrcSeqAccess,
                            ctrl._cpu.R[15],
                            channel.InternalLatch,
                            ctrl._cpu.Cycles,
                            false);
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

                if (channel.IntSourceAddress is >= 0x0800_0000 and < 0x0E00_0000)
                {
                    channel.IntDestSeqAccess = 1;
                    channel.IntSrcSeqAccess = 1;
                }
                channel.IntSourceAddress = (uint)(channel.IntSourceAddress + channel.IntSrcAddressIncrement); // TODO - Suspect I should be wrapping and masking this address

                _stepAction = &WriteCycle;
                _currentTimerIx = ii;

                // Only one DMA runs at a time in priority order from 0-3
                ctrl._bus.InUseByDma = true;
                return;
            }
        }
    }

    /// <summary>
    /// Steps the DMA controller and returns a boolean indicating whether DMA 
    /// is active (and therefore the CPU should be paused)
    /// </summary>
    /// 
    /// <returns>
    /// true if any DMA channel is active, false otherwise
    /// </returns>
    internal void Step()
    {
        _stepAction(this);
    }
}
