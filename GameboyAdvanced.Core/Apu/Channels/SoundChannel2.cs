namespace GameboyAdvanced.Core.Apu.Channels;

/// <summary>
/// Sound channel 2 is another square wave channel with a volume envelope but 
/// unlike channel 1 it has no sweep unit.
/// </summary>
internal class SoundChannel2 : ToneChannel
{
    internal SoundChannel2() : base(2) 
    {
        Reset();
    }

    internal override void Reset()
    {
        base.Reset();
    }

    internal override void Step()
    {
        //TODO
    }

    internal override ushort ReadControlH() => ReadFrequencyControl();

    internal override void WriteControlH(byte value, uint byteIndex) => WriteFrequencyControl(value, byteIndex);

    internal override ushort ReadControlL() => ReadDutyLengthEnvelope();

    internal override void WriteControlL(byte value, uint byteIndex)
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

    internal override ushort ReadControlX() => throw new Exception("Not CNT_X register for sound 2");
    internal override void WriteControlX(byte value, uint byteIndex) => throw new Exception("Not CNT_X register for sound 2");
}
