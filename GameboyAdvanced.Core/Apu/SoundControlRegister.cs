using GameboyAdvanced.Core.Apu.Channels;

namespace GameboyAdvanced.Core.Apu;

internal class SoundControlRegister
{
    internal int Sound1to4MasterVolumeRight;
    internal int Sound1to4MasterVolumeLeft;

    internal bool[] GbChannelEnableFlagsRight = new bool[4];
    internal bool[] GbChannelEnableFlagsLeft = new bool[4];

    internal int Sound1to4Volume;

    private readonly DmaChannel[] _dmaChannels;

    public SoundControlRegister(DmaChannel[] dmaChannels)
    {
        _dmaChannels = dmaChannels;
    }

    internal void Reset()
    {
        SetSoundCntL(0);
        SetSoundCntH(0);
    }

    internal void SetSoundCntL(ushort value)
    {
        Sound1to4MasterVolumeRight = value & 0b111;
        Sound1to4MasterVolumeLeft = (value >> 4) & 0b111;

        GbChannelEnableFlagsRight[0] = ((value >> 8) & 0b1) == 0b1;
        GbChannelEnableFlagsRight[1] = ((value >> 9) & 0b1) == 0b1;
        GbChannelEnableFlagsRight[2] = ((value >> 10) & 0b1) == 0b1;
        GbChannelEnableFlagsRight[3] = ((value >> 11) & 0b1) == 0b1;

        GbChannelEnableFlagsLeft[0] = ((value >> 12) & 0b1) == 0b1;
        GbChannelEnableFlagsLeft[1] = ((value >> 13) & 0b1) == 0b1;
        GbChannelEnableFlagsLeft[2] = ((value >> 14) & 0b1) == 0b1;
        GbChannelEnableFlagsLeft[3] = ((value >> 15) & 0b1) == 0b1;
    }

    internal ushort GetSoundCntL() => (ushort)(
        Sound1to4MasterVolumeRight | 
        (Sound1to4MasterVolumeLeft << 4) |
        (GbChannelEnableFlagsRight[0] ? (1 << 8) : 0) |
        (GbChannelEnableFlagsRight[1] ? (1 << 9) : 0) |
        (GbChannelEnableFlagsRight[2] ? (1 << 10) : 0) |
        (GbChannelEnableFlagsRight[3] ? (1 << 11) : 0) |
        (GbChannelEnableFlagsLeft[0] ? (1 << 12) : 0) |
        (GbChannelEnableFlagsLeft[1] ? (1 << 13) : 0) |
        (GbChannelEnableFlagsLeft[2] ? (1 << 14) : 0) |
        (GbChannelEnableFlagsLeft[3] ? (1 << 15) : 0));


    internal void SetSoundCntH(ushort value)
    {
        Sound1to4Volume = value & 0b11;
        _dmaChannels[0].FullVolume = ((value >> 2) & 0b1) == 0b1;
        _dmaChannels[1].FullVolume = ((value >> 3) & 0b1) == 0b1;

        _dmaChannels[0].EnableRight = ((value >> 8) & 0b1) == 0b1;
        _dmaChannels[0].EnableLeft = ((value >> 9) & 0b1) == 0b1;
        _dmaChannels[0].SelectTimer1 = ((value >> 10) & 0b1) == 0b1;
        if (((value >> 11) & 0b1) == 0b1)
        {
            _dmaChannels[0].ResetFifo();
        }

        _dmaChannels[1].EnableRight = ((value >> 12) & 0b1) == 0b1;
        _dmaChannels[1].EnableLeft = ((value >> 13) & 0b1) == 0b1;
        _dmaChannels[1].SelectTimer1 = ((value >> 14) & 0b1) == 0b1;
        if (((value >> 15) & 0b1) == 0b1)
        {
            _dmaChannels[1].ResetFifo();
        }
    }

    internal ushort GetSoundCntH()
    {
        return (ushort)(
            Sound1to4Volume |
            (_dmaChannels[0].FullVolume ? (1 << 2) : 0) |
            (_dmaChannels[1].FullVolume ? (1 << 3) : 0) |
            (_dmaChannels[0].EnableRight ? (1 << 8) : 0) |
            (_dmaChannels[0].EnableLeft ? (1 << 9) : 0) |
            (_dmaChannels[0].SelectTimer1 ? (1 << 10) : 0) |
            (_dmaChannels[1].EnableRight ? (1 << 12) : 0) |
            (_dmaChannels[1].EnableLeft ? (1 << 13) : 0) |
            (_dmaChannels[1].SelectTimer1 ? (1 << 14) : 0));
    }
}
