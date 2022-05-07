using GameboyAdvanced.Core.Interrupts;

namespace GameboyAdvanced.Core.Timer;

public unsafe class TimerRegister
{
    private readonly Device _device;

    public int Index;

    public ushort Reload;
    public ushort ReloadLatch;

    public TimerPrescaler PrescalerSelection;
    public TimerPrescaler PrescalerSelectionLatch;

    public bool CountUpTiming;
    public bool CountUpTimingLatch;

    public bool IrqEnabled;
    public bool IrqEnabledLatch;

    public bool OldStart;
    public bool Start;
    public bool StartLatch;

    public ushort CounterAtLastLatch;
    public long CyclesAtLastLatch;

    public TimerRegister(Device device, int index)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        Index = index;
        Reset();
    }

    internal ushort ReadCounter()
    {
        // Timers which are stopped or timers which use countup timing don't
        // need calculating as the counter value is fixed from the last time
        // it was set
        if (StartLatch && !CountUpTimingLatch)
        {
            var cyclesSinceLastLatch = _device.Cpu.Cycles - CyclesAtLastLatch;
            var increments = cyclesSinceLastLatch / PrescalerSelectionLatch.Cycles();

            return (ushort)(CounterAtLastLatch + increments);
        }
        else
        {
            return CounterAtLastLatch;
        }
    }

    /// <summary>
    /// Reading from the control register uses the pre-latched values. Latched 
    /// ones are only used by the timer internals
    /// </summary>
    internal ushort ReadControl() => (ushort)(
        (ushort)PrescalerSelection |
        (CountUpTiming ? (1 << 2) : 0) |
        (IrqEnabled ? (1 << 6) : 0) |
        (Start ? (1 << 7) : 0));

    private readonly static delegate*<Device, void>[] LatchEvents = new delegate*<Device, void>[]
    {
        &LatchTimer0ValuesEvent,
        &LatchTimer1ValuesEvent,
        &LatchTimer2ValuesEvent,
        &LatchTimer3ValuesEvent,
    };

    private readonly static EventType[] LatchEventTypes = new EventType[]
    {
        EventType.Timer0Latch,
        EventType.Timer1Latch,
        EventType.Timer2Latch,
        EventType.Timer3Latch,
    };

    private readonly static delegate*<Device, void>[] OverflowEvents = new delegate*<Device, void>[]
    {
        &Timer0OverflowEvent,
        &Timer1OverflowEvent,
        &Timer2OverflowEvent,
        &Timer3OverflowEvent,
    };

    private readonly static EventType[] OverflowEventTypes = new EventType[]
    {
        EventType.Timer0Overflow,
        EventType.Timer1Overflow,
        EventType.Timer2Overflow,
        EventType.Timer3Overflow,
    };

    private static void LatchTimerValuesEvent(Device device, int ix)
    {
        var timer = device.TimerController._timers[ix];
        timer.LatchValues();
    }

    internal static void LatchTimer0ValuesEvent(Device device) => LatchTimerValuesEvent(device, 0);

    internal static void LatchTimer1ValuesEvent(Device device) => LatchTimerValuesEvent(device, 1);

    internal static void LatchTimer2ValuesEvent(Device device) => LatchTimerValuesEvent(device, 2);

    internal static void LatchTimer3ValuesEvent(Device device) => LatchTimerValuesEvent(device, 3);

    internal readonly static EventType[] LatchReloadEventTypers = new EventType[]
    {
        EventType.Timer0LatchReload,
        EventType.Timer1LatchReload,
        EventType.Timer2LatchReload,
        EventType.Timer3LatchReload,
    };
    internal static void LatchTimerReloadEvent(Device device, int timerIx) => device.TimerController._timers[timerIx].ReloadLatch = device.TimerController._timers[timerIx].Reload;
    internal static void LatchTimer0ReloadEvent(Device device) => LatchTimerReloadEvent(device, 0);
    internal static void LatchTimer1ReloadEvent(Device device) => LatchTimerReloadEvent(device, 1);
    internal static void LatchTimer2ReloadEvent(Device device) => LatchTimerReloadEvent(device, 2);
    internal static void LatchTimer3ReloadEvent(Device device) => LatchTimerReloadEvent(device, 3);

    private readonly static EventType[] IrqSetEventTypes = new EventType[]
    {
        EventType.Timer0Irq,
        EventType.Timer1Irq,
        EventType.Timer2Irq,
        EventType.Timer3Irq
    };
    private readonly static delegate*<Device, void>[] IrqSetEvents = new delegate*<Device, void>[]
    {
        &Timer0IrqSet,
        &Timer1IrqSet,
        &Timer2IrqSet,
        &Timer3IrqSet,
    };
    internal static void Timer0IrqSet(Device device) => device.InterruptInterconnect.RaiseInterrupt(Interrupt.Timer0Overflow);
    internal static void Timer1IrqSet(Device device) => device.InterruptInterconnect.RaiseInterrupt(Interrupt.Timer1Overflow);
    internal static void Timer2IrqSet(Device device) => device.InterruptInterconnect.RaiseInterrupt(Interrupt.Timer2Overflow);
    internal static void Timer3IrqSet(Device device) => device.InterruptInterconnect.RaiseInterrupt(Interrupt.Timer3Overflow);

    private static void TimerOverflowEvent(Device device, int ix)
    {
        var timer = device.TimerController._timers[ix];

        // Handle count up timers by checking if the next timer is both
        // counting up _and_ started
        if (ix < 3)
        {
            if (device.TimerController._timers[ix + 1].CountUpTimingLatch && device.TimerController._timers[ix + 1].StartLatch)
            {
                device.TimerController._timers[ix + 1].CounterAtLastLatch++;
                device.TimerController._timers[ix + 1].CyclesAtLastLatch = device.Cpu.Cycles;

                if (device.TimerController._timers[ix + 1].CounterAtLastLatch == 0)
                {
                    device.TimerController._timers[ix + 1].CounterAtLastLatch = device.TimerController._timers[ix + 1].ReloadLatch;
                    TimerOverflowEvent(device, ix + 1);
                }
            }
        }

        var cyclesToOverflow = timer.PrescalerSelectionLatch.Cycles() * (0x1_0000 - timer.ReloadLatch);

        if (!timer.CountUpTimingLatch)
        {
            timer.CounterAtLastLatch = timer.ReloadLatch;
            timer.CyclesAtLastLatch = device.Cpu.Cycles + 1; // TODO - Why +1?
            device.Scheduler.CancelEvent(OverflowEventTypes[ix]);
            device.Scheduler.ScheduleEvent(OverflowEventTypes[ix], OverflowEvents[ix], cyclesToOverflow);
        }

        if (timer.IrqEnabledLatch)
        {
            device.Scheduler.ScheduleEvent(IrqSetEventTypes[ix], IrqSetEvents[ix], 1);
        }
    }

    internal static void Timer0OverflowEvent(Device device) => TimerOverflowEvent(device, 0);

    internal static void Timer1OverflowEvent(Device device) => TimerOverflowEvent(device, 1);

    internal static void Timer2OverflowEvent(Device device) => TimerOverflowEvent(device, 2);

    internal static void Timer3OverflowEvent(Device device) => TimerOverflowEvent(device, 3);

    internal void UpdateControl(ushort value)
    {
        PrescalerSelection = (TimerPrescaler)(value & 0b11);
        CountUpTiming = (value & (1 << 2)) == (1 << 2);
        IrqEnabled = (value & (1 << 6)) == (1 << 6);
        OldStart = Start;
        Start = (value & (1 << 7)) == (1 << 7);

        _device.Scheduler.ScheduleEvent(LatchEventTypes[Index], LatchEvents[Index], 1);
    }

    internal void Reset()
    {
        Reload = 0;
        ReloadLatch = 0;
        PrescalerSelection = TimerPrescaler.F_1;
        PrescalerSelectionLatch = TimerPrescaler.F_1;
        CountUpTiming = false;
        CountUpTimingLatch = false;
        IrqEnabled = false;
        IrqEnabledLatch = false;
        Start = false;
        StartLatch = false;
    }

    public override string ToString() => $"{Index} - Reload {Reload:X4} - Control {ReadControl():X4}";

    /// <summary>
    /// All the timer register values are latched on the cycle after they're 
    /// written to as seen by the timer controller.
    /// </summary>
    internal void LatchValues()
    {
        CountUpTimingLatch = CountUpTiming && Index != 0;
        IrqEnabledLatch = IrqEnabled;
        PrescalerSelectionLatch = PrescalerSelection;

        if (Start && !OldStart)
        {
            CounterAtLastLatch = Reload;
            // Add 1 here as the timer doesn't start for 2 cycles after writing
            // (1 for this latch and one for start delay)
            CyclesAtLastLatch = _device.Cpu.Cycles + 1;

            _device.Scheduler.CancelEvent(OverflowEventTypes[Index]);
            if (!CountUpTimingLatch)
            {
                // Any latch causes us to recalculate the cycles until the timer
                // overflows, this call reschedules the event for the new number
                // of cycles
                var cyclesToOverflow = PrescalerSelectionLatch.Cycles() * (0x1_0000 - Reload);
                _device.Scheduler.ScheduleEvent(OverflowEventTypes[Index], OverflowEvents[Index], cyclesToOverflow);
            }
        }
        else
        {
            CounterAtLastLatch = ReadCounter();
            CyclesAtLastLatch = _device.Cpu.Cycles;

            if (!Start)
            {
                _device.Scheduler.CancelEvent(OverflowEventTypes[Index]);
            }
        }

        ReloadLatch = Reload;
        StartLatch = Start;
        OldStart = Start;
    }
}
