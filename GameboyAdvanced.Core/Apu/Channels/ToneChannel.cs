using GameboyAdvanced.Core.Apu.Units;

namespace GameboyAdvanced.Core.Apu.Channels;

internal abstract class ToneChannel : GBSoundChannel
{
    readonly protected Envelope _envelope = new();

    protected int _frequency;
    protected int _length;
    protected int _dutyPattern;

    internal ToneChannel(int index) : base(index) { }

    protected void WriteDutyLengthEnvelope(ushort value)
    {
        _length = value & 0b11_1111;
        _dutyPattern = (value >> 6) & 0b11;
        _envelope.Set((byte)(value >> 8));
    }

    protected ushort ReadDutyLengthEnvelope() => (ushort)((_dutyPattern << 6) | (_envelope.Get() << 8));

    protected void WriteFrequencyControl(ushort value)
    {
        _frequency = value & 0b0111_1111_1111;
        _lengthFlag = ((value >> 14) & 0b1) == 0b1;
        _restartScheduled = ((value >> 15) & 0b1) == 0b1;
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
