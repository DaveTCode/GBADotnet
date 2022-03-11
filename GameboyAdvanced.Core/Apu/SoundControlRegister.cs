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
        SetSoundCntL(0, 0);
        SetSoundCntL(0, 1);
        SetSoundCntH(0, 0);
        SetSoundCntH(0, 1);
    }

    internal void SetSoundCntL(byte value, uint byteIndex)
    {
        switch (byteIndex)
        {
            case 0:
                Sound1to4MasterVolumeRight = value & 0b111;
                Sound1to4MasterVolumeLeft = (value >> 4) & 0b111;
                break;
            default:
                GbChannelEnableFlagsRight[0] = (value & 0b1) == 0b1;
                GbChannelEnableFlagsRight[1] = ((value >> 1) & 0b1) == 0b1;
                GbChannelEnableFlagsRight[2] = ((value >> 2) & 0b1) == 0b1;
                GbChannelEnableFlagsRight[3] = ((value >> 3) & 0b1) == 0b1;

                GbChannelEnableFlagsLeft[0] = ((value >> 4) & 0b1) == 0b1;
                GbChannelEnableFlagsLeft[1] = ((value >> 5) & 0b1) == 0b1;
                GbChannelEnableFlagsLeft[2] = ((value >> 6) & 0b1) == 0b1;
                GbChannelEnableFlagsLeft[3] = ((value >> 7) & 0b1) == 0b1;
                break;
        }
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


    internal void SetSoundCntH(byte value, uint byteIndex)
    {
        switch (byteIndex)
        {
            case 0:
                Sound1to4Volume = value & 0b11;
                _dmaChannels[0].FullVolume = ((value >> 2) & 0b1) == 0b1;
                _dmaChannels[1].FullVolume = ((value >> 3) & 0b1) == 0b1;
                break;
            case 1:
                _dmaChannels[0].EnableRight = (value & 0b1) == 0b1;
                _dmaChannels[0].EnableLeft = ((value >> 1) & 0b1) == 0b1;
                _dmaChannels[0].SelectTimer1 = ((value >> 2) & 0b1) == 0b1;
                if (((value >> 3) & 0b1) == 0b1)
                {
                    _dmaChannels[0].ResetFifo();
                }

                _dmaChannels[1].EnableRight = ((value >> 4) & 0b1) == 0b1;
                _dmaChannels[1].EnableLeft = ((value >> 5) & 0b1) == 0b1;
                _dmaChannels[1].SelectTimer1 = ((value >> 6) & 0b1) == 0b1;
                if (((value >> 7) & 0b1) == 0b1)
                {
                    _dmaChannels[1].ResetFifo();
                }
                break;
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
