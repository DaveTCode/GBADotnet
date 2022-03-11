using GameboyAdvanced.Core.Apu.Units;

namespace GameboyAdvanced.Core.Apu.Channels;

/// <summary>
/// Sound channel 1 is common between GB and GBA and is a square wave channel
/// with frequency sweep and volume envelope functionality.
/// </summary>
internal class SoundChannel1 : ToneChannel
{
    private readonly SweepUnit _sweepUnit = new();

    internal SoundChannel1() : base(1) { Reset(); }

    internal override void WriteControlL(ushort value) => _sweepUnit.Set(value);

    internal override ushort ReadControlL() => _sweepUnit.Get();

    internal override ushort ReadControlH() => ReadDutyLengthEnvelope();

    internal override void WriteControlH(ushort value) => WriteDutyLengthEnvelope(value);

    internal override ushort ReadControlX() => ReadFrequencyControl();

    internal override void WriteControlX(ushort value) => WriteFrequencyControl(value);

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
