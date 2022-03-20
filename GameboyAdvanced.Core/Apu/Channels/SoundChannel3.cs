namespace GameboyAdvanced.Core.Apu.Channels;

/// <summary>
/// Sound Channel 3 plays a wave buffer of 4 bit samples
/// </summary>
public class SoundChannel3 : GBSoundChannel
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

    internal override void WriteControlH(byte value, uint byteIndex)
    {
        switch (byteIndex)
        {
            case 0:
                _length = value & 0xF;
                break;
            default:
                _volume = (value >> 5) & 0b11;
                _force75PctVolume = (value >> 7) == 1;
                break;
        }
    }

    internal override ushort ReadControlL() =>
        (ushort)((_isTwoBankRam ? (1 << 5) : 0) | (_waveRamBankIndex << 6) | (_on ? (1 << 7) : 0));

    internal override void WriteControlL(byte value, uint byteIndex)
    {
        if (byteIndex == 0)
        {
            _isTwoBankRam = ((value >> 5) & 1) == 1;
            _waveRamBankIndex = (value >> 6) & 1;
            _on = ((value >> 7) & 1) == 1;
        }
    }

    internal override ushort ReadControlX() => (ushort)(_lengthFlag ? (1 << 14) : 0);

    internal override void WriteControlX(byte value, uint byteIndex)
    {
        switch (byteIndex)
        {
            case 0:
                _sampleRate = (_sampleRate & 0xFF00) | value;
                break;
            default:
                _sampleRate = (_sampleRate & 0xFF) | ((value & 0b111) << 8);
                _lengthFlag = ((value >> 6) & 0b1) == 0b1;
                _restartScheduled = ((value >> 7) & 0b1) == 0b1;
                break;
        }
    }

    internal void WriteWaveRamByte(uint address, byte value)
    {
        _waveRamBanks[_waveRamBankIndex ^ 1][address & 0xF] = value;
    }

    internal ushort ReadWaveRam(uint alignedAddress) =>
        Utils.ReadHalfWord(_waveRamBanks[_waveRamBankIndex ^ 1], alignedAddress, 0xF);
}
