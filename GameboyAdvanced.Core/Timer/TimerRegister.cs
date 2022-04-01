namespace GameboyAdvanced.Core.Timer;

public struct TimerRegister
{
    /// <summary>
    /// All the timer register values are latched on the cycle after they're 
    /// written to. To emulate that behaviour we have a single boolean which
    /// tracks whether a latch is required. This is checked on each cycle.
    /// </summary>
    public bool NeedsLatch;

    public int Index;

    /// <summary>
    /// This is the internal counter of the timer
    /// </summary>
    public ushort Counter;

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

    public int CyclesToStart;

    public TimerRegister(int index)
    {
        Index = index;
        Reload = 0;
        ReloadLatch = 0;
        NeedsLatch = false;
        Counter = 0;
        PrescalerSelection = TimerPrescaler.F_1;
        PrescalerSelectionLatch = TimerPrescaler.F_1;
        CountUpTiming = false;
        CountUpTimingLatch = false;
        IrqEnabled = false;
        IrqEnabledLatch = false;
        Start = false;
        StartLatch = false;
        CyclesToStart = 0;
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

    internal void UpdateControl(ushort value)
    {
        NeedsLatch = true;
        PrescalerSelection = (TimerPrescaler)(value & 0b11);
        CountUpTiming = (value & (1 << 2)) == (1 << 2);
        IrqEnabled = (value & (1 << 6)) == (1 << 6);

        var oldStart = Start;
        Start = (value & (1 << 7)) == (1 << 7);
        if (Start && !oldStart)
        {
            Counter = Reload;
            CyclesToStart = 1; // 2 cycle startup delay but 1 is handled by latching start on the next cycle
        }
    }

    internal void Reset()
    {
        Reload = 0;
        ReloadLatch = 0;
        NeedsLatch = false;
        Counter = 0;
        PrescalerSelection = TimerPrescaler.F_1;
        PrescalerSelectionLatch = TimerPrescaler.F_1;
        CountUpTiming = false;
        CountUpTimingLatch = false;
        IrqEnabled = false;
        IrqEnabledLatch = false;
        Start = false;
        StartLatch = false;
        CyclesToStart = 0;
    }

    public override string ToString() => $"{Index} - Reload {Reload:X4} - Control {ReadControl():X4} - Counter {Counter:X4}";

    internal void LatchValues(ref int timerSteps)
    {
        ReloadLatch = Reload;
        CountUpTimingLatch = CountUpTiming;
        IrqEnabledLatch = IrqEnabled;
        PrescalerSelectionLatch = PrescalerSelection;
        timerSteps = PrescalerSelectionLatch.Cycles();
        StartLatch = Start;
        NeedsLatch = false;
    }
}
