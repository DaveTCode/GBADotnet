namespace GameboyAdvanced.Core.Timer;

internal struct TimerRegister
{ 
    internal int Index;
    internal ushort Reload;
    internal ushort Counter;
    internal TimerPrescaler PrescalerSelection;
    internal bool CountUpTiming;
    internal bool IrqEnabled;
    internal bool Start;

    public TimerRegister(int index)
    {
        Index = index;
        Reload = 0;
        Counter = 0;
        PrescalerSelection = TimerPrescaler.F_1;
        CountUpTiming = false;
        IrqEnabled = false;
        Start = false;
    }

    internal ushort ReadControl() => (ushort)(
        (ushort)PrescalerSelection |
        (CountUpTiming ? (1 << 2) : 0) |
        (IrqEnabled ? (1 << 6) : 0) |
        (Start ? (1 << 7) : 0));

    internal void UpdateControl(ushort value, HashSet<int> runningTimerIxs)
    {
        PrescalerSelection = (TimerPrescaler)(value & 0b11);
        CountUpTiming = (value & (1 << 2)) == (1 << 2);
        IrqEnabled = (value & (1 << 6)) == (1 << 6);

        var oldStart = Start;
        Start = (value & (1 << 7)) == (1 << 7);
        if (Start)
        {
            if (!oldStart)
            {
                Counter = Reload;
            }
            _ = runningTimerIxs.Add(Index);
        }
        else
        {
            _ = runningTimerIxs.Remove(Index);
        }
    }

    internal void Reset()
    {
        Reload = 0;
        Counter= 0;
        PrescalerSelection = TimerPrescaler.F_1;
        CountUpTiming = false;
        IrqEnabled = false;
        Start = false;
    }
}
