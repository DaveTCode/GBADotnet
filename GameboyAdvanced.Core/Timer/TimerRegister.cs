namespace GameboyAdvanced.Core.Timer;

internal struct TimerRegister
{ 
    internal ushort Reload;
    internal ushort Counter;
    internal int PrescalerSelection;
    internal bool CountUpTiming;
    internal bool IrqEnabled;
    internal bool Start;

    internal ushort ReadControl() => (ushort)(
        (ushort)PrescalerSelection |
        (CountUpTiming ? (1 << 2) : 0) |
        (IrqEnabled ? (1 << 6) : 0) |
        (Start ? (1 << 7) : 0));

    internal void UpdateControl(ushort value)
    {
        PrescalerSelection = value & 0b11;
        CountUpTiming = (value & (1 << 2)) == (1 << 2);
        IrqEnabled = (value & (1 << 6)) == (1 << 6);

        var oldStart = Start;
        Start = (value & (1 << 7)) == (1 << 7);
        if (Start && !oldStart)
        {
            Counter = Reload;
        }
    }
}
