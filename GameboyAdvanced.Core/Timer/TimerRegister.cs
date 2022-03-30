namespace GameboyAdvanced.Core.Timer;

public struct TimerRegister
{
    public int Index;
    public ushort Reload;
    public bool ReloadNeedsLatch;
    public ushort ReloadLatch;
    public ushort Counter;
    public TimerPrescaler PrescalerSelection;
    public bool CountUpTiming;
    public bool IrqEnabled;
    public bool Start;
    public int CyclesToStart;
    public int CyclesToStop;

    public TimerRegister(int index)
    {
        Index = index;
        Reload = 0;
        ReloadNeedsLatch = false;
        ReloadLatch = 0;
        Counter = 0;
        PrescalerSelection = TimerPrescaler.F_1;
        CountUpTiming = false;
        IrqEnabled = false;
        Start = false;
        CyclesToStart = 0;
        CyclesToStop = 0;
    }

    internal ushort ReadControl() => (ushort)(
        (ushort)PrescalerSelection |
        (CountUpTiming ? (1 << 2) : 0) |
        (IrqEnabled ? (1 << 6) : 0) |
        (Start ? (1 << 7) : 0));

    internal void UpdateControl(ushort value)
    {
        PrescalerSelection = (TimerPrescaler)(value & 0b11);
        CountUpTiming = (value & (1 << 2)) == (1 << 2);
        IrqEnabled = (value & (1 << 6)) == (1 << 6);

        var oldStart = Start;
        Start = (value & (1 << 7)) == (1 << 7);
        if (Start && !oldStart)
        {
            Counter = Reload;
            CyclesToStart = 2; // 2 cycle startup delay
        }
        else if (!Start && oldStart)
        {
            CyclesToStop = 1; // 1 cycle delay for register write to stop timer
        }
    }

    internal void SetReload(ushort value)
    {
        Reload = value;
        ReloadNeedsLatch = true;
    }

    internal void Reset()
    {
        Reload = 0;
        ReloadNeedsLatch = false;
        ReloadLatch = 0;
        Counter = 0;
        PrescalerSelection = TimerPrescaler.F_1;
        CountUpTiming = false;
        IrqEnabled = false;
        Start = false;
        CyclesToStart = 0;
        CyclesToStop = 0;
    }

    public override string ToString() => $"{Index} - Reload {Reload:X4} - Control {ReadControl():X4} - Counter {Counter:X4}";
}
