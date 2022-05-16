using GameboyAdvanced.Core.Apu.Units;

namespace GameboyAdvanced.Core.Apu.Channels;

/// <summary>
/// Channel 4 is used to produce white noise at various frequencies.
/// </summary>
public class SoundChannel4 : GBSoundChannel
{
    public readonly Envelope _envelope = new();

    public int _length;
    public int _ratio;
    public bool _isShortWidth;
    public int _shiftClockFrequency;

    public SoundChannel4() : base(4)
    {
        Reset();
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

    internal override void WriteControlL(byte value, uint byteIndex)
    {
        switch (byteIndex)
        {
            case 0:
                _length = value & 0b11_1111;
                break;
            default:
                _envelope.Set(value);
                break;
        }
    }

    internal override ushort ReadControlH() =>
        (ushort)(_ratio | (_isShortWidth ? (1 << 3) : 0) | (_shiftClockFrequency << 4) | (_lengthFlag ? (1 << 14) : 0));

    internal override void WriteControlH(byte value, uint byteIndex)
    {
        switch (byteIndex)
        {
            case 0:
                _ratio = value & 0b111;
                _isShortWidth = ((value >> 3) & 0b1) == 0b1;
                _shiftClockFrequency = (value >> 4) & 0b1111;
                break;
            default:
                _lengthFlag = ((value >> 6) & 0b1) == 0b1;
                _restartScheduled = ((value >> 7) & 0b1) == 0b1;
                break;
        }
    }

    internal override ushort ReadControlX() => throw new Exception("No CNT_X register for channel 4");

    internal override void WriteControlX(byte value, uint byteIndex) => throw new Exception("No CNT_X register for channel 4");
}
