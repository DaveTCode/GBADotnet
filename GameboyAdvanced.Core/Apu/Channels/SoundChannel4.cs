using GameboyAdvanced.Core.Apu.Units;

namespace GameboyAdvanced.Core.Apu.Channels;

/// <summary>
/// Channel 4 is used to produce white noise at various frequencies.
/// </summary>
internal class SoundChannel4 : GBSoundChannel
{
    private readonly Envelope _envelope = new();

    private int _length;
    private int _ratio;
    private bool _isShortWidth;
    private int _shiftClockFrequency;

    internal SoundChannel4() : base(4)
    {
        Reset();
    }

    internal override void Step()
    {
        // TODO
    }

    internal override void Reset()
    {
        _envelope.Reset();
        _length = 0;
        _ratio = 0;
        _isShortWidth = false;
        _shiftClockFrequency = 0;
        _lengthFlag = false;
        _restartScheduled = false;
    }

    internal override ushort ReadControlL() => (ushort)(_envelope.Get() << 8);

    internal override void WriteControlL(ushort value)
    {
        _length = value & 0b11_1111;
        _envelope.Set((byte)(value >> 8));
    }

    internal override ushort ReadControlH() =>
        (ushort)(_ratio | (_isShortWidth ? (1 << 3) : 0) | (_shiftClockFrequency << 4) | (_lengthFlag ? (1 << 14) : 0));

    internal override void WriteControlH(ushort value)
    {
        _ratio = value & 0b111;
        _isShortWidth = ((value >> 3) & 0b1) == 0b1;
        _shiftClockFrequency = (value >> 4) & 0b1111;
        _lengthFlag = ((value >> 14) & 0b1) == 0b1;
        _restartScheduled = ((value >> 15) & 0b1) == 0b1;
    }

    internal override ushort ReadControlX() => throw new Exception("No CNT_X register for channel 4");

    internal override void WriteControlX(ushort value) => throw new Exception("No CNT_X register for channel 4");
}
