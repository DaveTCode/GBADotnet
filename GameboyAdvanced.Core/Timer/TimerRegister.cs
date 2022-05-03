﻿using GameboyAdvanced.Core.Interrupts;

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
            increments -= (CounterAtLastLatch - ReloadLatch);

            return (ushort)(ReloadLatch + (increments % (0x1_0000 - ReloadLatch)));
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
        timer.LatchValues(ref device.TimerController._timerSteps[ix]);
    }

    internal static void LatchTimer0ValuesEvent(Device device) => LatchTimerValuesEvent(device, 0);

    internal static void LatchTimer1ValuesEvent(Device device) => LatchTimerValuesEvent(device, 1);

    internal static void LatchTimer2ValuesEvent(Device device) => LatchTimerValuesEvent(device, 2);

    internal static void LatchTimer3ValuesEvent(Device device) => LatchTimerValuesEvent(device, 3);

    private static void TimerOverflowEvent(Device device, int ix)
    {
        device.TimerController._timerSteps[ix] = device.TimerController._timers[ix].PrescalerSelectionLatch.Cycles();
        var timer = device.TimerController._timers[ix];

        if (timer.IrqEnabledLatch)
        {
            device.InterruptInterconnect.RaiseInterrupt(ix switch
            {
                0 => Interrupt.Timer0Overflow,
                1 => Interrupt.Timer1Overflow,
                2 => Interrupt.Timer2Overflow,
                3 => Interrupt.Timer3Overflow,
                _ => throw new Exception("Invalid timer ix"),
            });
        }

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
                    TimerOverflowEvent(device, ix + 1);
                }
            }
        }

        var cyclesToOverflow = timer.PrescalerSelectionLatch.Cycles() * (0x1_0000 - timer.ReloadLatch);

        if (!timer.CountUpTimingLatch)
        {
            device.Scheduler.ScheduleEvent(OverflowEventTypes[ix], OverflowEvents[ix], cyclesToOverflow);
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
        var oldStart = Start;
        Start = (value & (1 << 7)) == (1 << 7);

        if (Start && !oldStart)
        {
            CounterAtLastLatch = Reload;
            // Add 2 here as the timer doesn't start for 2 cycles after writing
            // (1 for latch and one for start delay)
            CyclesAtLastLatch = _device.Cpu.Cycles + 2; 
        }

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
    internal void LatchValues(ref int timerSteps)
    {
        ReloadLatch = Reload;
        CountUpTimingLatch = CountUpTiming;
        IrqEnabledLatch = IrqEnabled;
        PrescalerSelectionLatch = PrescalerSelection;
        timerSteps = PrescalerSelectionLatch.Cycles();

        if (Start && !CountUpTimingLatch)
        {
            var cyclesToOverflow = PrescalerSelectionLatch.Cycles() * (0x1_0000 - ReloadLatch);

            // Any latch causes us to recalculate the cycles until the timer
            // overflows, this call reschedules the event for the new number
            // of cycles
            _device.Scheduler.ScheduleEvent(OverflowEventTypes[Index], OverflowEvents[Index], cyclesToOverflow);
        }
        else
        {
            CounterAtLastLatch = ReadCounter();
            CyclesAtLastLatch = _device.Cpu.Cycles;
            _device.Scheduler.CancelEvent(OverflowEventTypes[Index]);
        }

        StartLatch = Start;
    }
}
