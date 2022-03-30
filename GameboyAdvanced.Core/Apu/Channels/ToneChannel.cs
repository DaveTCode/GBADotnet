using GameboyAdvanced.Core.Apu.Units;

namespace GameboyAdvanced.Core.Apu.Channels;

public abstract class ToneChannel : GBSoundChannel
{
    public readonly Envelope _envelope = new();

    public int _frequency;
    public int _length;
    public int _dutyPattern;

    internal ToneChannel(int index) : base(index) { }

    protected void WriteDutyLength(byte value)
    {
        _length = value & 0b11_1111;
        _dutyPattern = (value >> 6) & 0b11;
    }

    protected ushort ReadDutyLengthEnvelope() => (ushort)((_dutyPattern << 6) | (_envelope.Get() << 8));

    protected void WriteFrequencyControl(byte value, uint byteIndex)
    {
        switch (byteIndex)
        {
            case 0:
                _frequency = (_frequency & 0xFF00) | value;
                break;
            default:
                _frequency = (_frequency & 0xFF) | ((value & 0b111) << 8);
                _lengthFlag = ((value >> 6) & 0b1) == 0b1;
                _restartScheduled = ((value >> 7) & 0b1) == 0b1;
                break;
        }
    }

    protected ushort ReadFrequencyControl() => (ushort)(_lengthFlag ? (1 << 14) : 0);

    internal override void Reset()
    {
        _envelope.Reset();
        _frequency = 0;
        _length = 0;
        _lengthFlag = false;
        _dutyPattern = 0;
        _restartScheduled = false;
    }
}
