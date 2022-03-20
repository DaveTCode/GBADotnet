namespace GameboyAdvanced.Core.Apu.Units;

public class Envelope
{
    internal int EnvelopeStepTime;

    internal bool IsIncrease;
    
    internal int InitialVolume;

    internal void Reset()
    {
        Set(0);
    }

    internal void Set(byte value)
    {
        EnvelopeStepTime = value & 0b111;
        IsIncrease = (value & 0b1000) == 0b1000;
        InitialVolume = (value >> 4) & 0b1111;
    }

    internal byte Get() => (byte)(EnvelopeStepTime | (IsIncrease ? 0b1000 : 0) | (InitialVolume << 4));
}
