namespace GameboyAdvanced.Core.Apu.Channels;

/// <summary>
/// Sound Channel 3 plays a wave buffer of 4 bit samples
/// </summary>
internal class SoundChannel3 : GBSoundChannel
{
    private readonly byte[][] _waveRamBanks = new byte[2][];

    private int _length;
    private bool _isTwoBankRam;
    private int _waveRamBankIndex;
    private bool _on;

    private int _volume;
    private bool _force75PctVolume;
    private int _sampleRate;
    private bool _lengthFlag;
    private bool _restartScheduled;

    internal SoundChannel3() : base(3)
    {
        _waveRamBanks[0] = new byte[16];
        _waveRamBanks[1] = new byte[16];
        Reset();
    }

    internal override void Reset()
    {
        Array.Clear(_waveRamBanks[0]);
        Array.Clear(_waveRamBanks[1]);
        _isTwoBankRam = false;
        _waveRamBankIndex = 0;
        _on = false;
        _length = 0;
        _volume = 0;
        _force75PctVolume = false;
        _sampleRate = 0;
        _lengthFlag = false;
        _restartScheduled = false;
    }

    internal override void Step()
    {
        // TODO
    }

    internal override ushort ReadControlH() =>
        (ushort)((_volume << 13) | (_force75PctVolume ? (1 << 15) : 0));

    internal override void WriteControlH(ushort value)
    {
        _length = value & 0XF;
        _volume = (value >> 13) & 0b11;
        _force75PctVolume = (value >> 15) == 1;
    }

    internal override ushort ReadControlL() =>
        (ushort)((_isTwoBankRam ? (1 << 5) : 0) | (_waveRamBankIndex << 6) | (_on ? (1 << 7) : 0));

    internal override void WriteControlL(ushort value)
    {
        _isTwoBankRam = ((value >> 5) & 1) == 1;
        _waveRamBankIndex = (value >> 6) & 1;
        _on = ((value >> 7) & 1) == 1;
    }

    internal override ushort ReadControlX() => (ushort)(_lengthFlag ? (1 << 14) : 0);

    internal override void WriteControlX(ushort value)
    {
        _sampleRate = value & 0b111_1111_1111;
        _lengthFlag = ((value >> 14) & 0b1) == 0b1;
        _restartScheduled = ((value >> 15) & 0b1) == 0b1;
    }

    internal void WriteWaveRam(uint alignedAddress, ushort value)
    {
        Utils.WriteHalfWord(_waveRamBanks[_waveRamBankIndex ^ 1], 0xF, alignedAddress, value);
    }

    internal ushort ReadWaveRam(uint alignedAddress) =>
        Utils.ReadHalfWord(_waveRamBanks[_waveRamBankIndex ^ 1], alignedAddress, 0xF);
}
