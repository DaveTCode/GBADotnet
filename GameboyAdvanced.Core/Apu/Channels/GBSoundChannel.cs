namespace GameboyAdvanced.Core.Apu.Channels;

internal abstract class BaseChannel
{
    internal int Index;

    internal BaseChannel(int index)
    {
        Index = index;
    }

    internal abstract void Step();

    internal abstract void Reset();
}

internal abstract class GBSoundChannel : BaseChannel
{
    protected bool _lengthFlag;
    protected bool _restartScheduled;

    internal GBSoundChannel(int index) : base(index) {}

    internal abstract ushort ReadControlL();

    internal abstract void WriteControlL(ushort value);

    internal abstract ushort ReadControlH();

    internal abstract void WriteControlH(ushort value);

    internal abstract ushort ReadControlX();

    internal abstract void WriteControlX(ushort value);
}
