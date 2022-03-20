using GameboyAdvanced.Core.Apu.Units;

namespace GameboyAdvanced.Core.Apu.Channels;

/// <summary>
/// Sound channel 1 is common between GB and GBA and is a square wave channel
/// with frequency sweep and volume envelope functionality.
/// </summary>
public class SoundChannel1 : ToneChannel
{
    private readonly SweepUnit _sweepUnit = new();

    internal SoundChannel1() : base(1) { Reset(); }

    internal override ushort ReadControlL() => _sweepUnit.Get();

    internal override void WriteControlL(byte value, uint byteIndex)
    {
        if (byteIndex == 0)
        {
            _sweepUnit.Set(value);
        }
    }

    internal override ushort ReadControlH() => ReadDutyLengthEnvelope();

    internal override void WriteControlH(byte value, uint byteIndex)
    {
        switch (byteIndex)
        {
            case 0:
                WriteDutyLength(value);
                break;
            default:
                _envelope.Set(value);
                break;
        }
    }

    internal override ushort ReadControlX() => ReadFrequencyControl();

    internal override void WriteControlX(byte value, uint byteIndex) => WriteFrequencyControl(value, byteIndex);

    internal override void Reset()
    {
        _sweepUnit.Reset();
        base.Reset();
    }

    internal override void Step()
    {
        // TODO
    }
}
