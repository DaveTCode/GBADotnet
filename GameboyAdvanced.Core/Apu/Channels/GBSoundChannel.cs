namespace GameboyAdvanced.Core.Apu.Channels;

public abstract class BaseChannel
{
    internal int Index;

    internal BaseChannel(int index)
    {
        Index = index;
    }

    internal abstract void Step();

    internal abstract void Reset();
}

public abstract class GBSoundChannel : BaseChannel
{
    protected bool _lengthFlag;
    protected bool _restartScheduled;

    internal GBSoundChannel(int index) : base(index) {}

    internal abstract ushort ReadControlL();

    internal abstract void WriteControlL(byte value, uint byteIndex);

    internal abstract ushort ReadControlH();

    internal abstract void WriteControlH(byte value, uint byteIndex);

    internal abstract ushort ReadControlX();

    internal abstract void WriteControlX(byte value, uint byteIndex);
}
