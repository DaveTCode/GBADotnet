namespace GameboyAdvanced.Core.Apu.Units;

internal class SweepUnit
{
    internal int NumberOfSweepShift;
    internal bool IsDecrease;
    private int _sweepTime;

    internal void Reset()
    {
        Set(0);
    }

    internal void Set(byte val)
    {
        NumberOfSweepShift = val & 0b111;
        IsDecrease = (val & 0b1000) == 0b1000;
        _sweepTime = (val >> 4) & 0b111;
    }

    internal ushort Get()
    {
        return (ushort)(NumberOfSweepShift
            | (IsDecrease ? 0b1000 : 0)
            | (_sweepTime << 4));
    }
}
