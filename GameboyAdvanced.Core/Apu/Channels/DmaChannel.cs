namespace GameboyAdvanced.Core.Apu.Channels;

public class DmaChannel : BaseChannel
{
    public bool FullVolume;
    public bool EnableRight;
    public bool EnableLeft;
    public bool SelectTimer1;

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
