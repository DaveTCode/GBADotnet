namespace GameboyAdvanced.Core.Apu.Channels;

internal class DmaChannel : BaseChannel
{
    internal bool FullVolume;
    internal bool EnableRight;
    internal bool EnableLeft;
    internal bool SelectTimer1;

    public DmaChannel(int index) : base(index)
    {
    }

    internal override void Reset()
    {
        FullVolume = false;
        EnableRight = false;
        EnableLeft = false;
        SelectTimer1 = false;
    }

    internal void ResetFifo()
    {
        // TODO
    }

    internal override void Step()
    {
        // TODO
    }
}
