using GameboyAdvanced.Core.Apu.Channels;
using GameboyAdvanced.Core.Debug;
using static GameboyAdvanced.Core.IORegs;

namespace GameboyAdvanced.Core.Apu;

internal class Apu
{
    private readonly BaseDebugger _debugger;

    private readonly DmaChannel[] _dmaChannels = new DmaChannel[2]
    {
        new DmaChannel(1), new DmaChannel(2),
    };
    private readonly GBSoundChannel[] _channels = new GBSoundChannel[4]
    {
        new SoundChannel1(), 
        new SoundChannel2(), 
        new SoundChannel3(),
        new SoundChannel4(),
    };
    private readonly SoundControlRegister _soundControlRegister;
    private bool _psgFifoMasterEnable;
    private int _biasLevel;
    private int _samplingCycle;

    internal Apu(BaseDebugger debugger)
    {
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _soundControlRegister = new SoundControlRegister(_dmaChannels);
    }

    internal void Reset()
    {
        _soundControlRegister.Reset();
        foreach (var channel in _channels)
        {
            channel.Reset();
        }
        _psgFifoMasterEnable = false;
        WriteHalfWord(SOUNDBIAS, 0x200);
    }
    
    private ushort SoundCntX()
    {
        // TODO - Return whether sounds are currently playing as well
        return (ushort)(_psgFifoMasterEnable ? (1 << 7) : 0);
    }

    internal byte ReadByte(uint address, uint openbus) => address switch
    {
        _ => throw new NotImplementedException("Byte reads from APU registers not implemented")
    };

    internal ushort ReadHalfWord(uint alignedAddress, uint openbus) => alignedAddress switch
    {
        SOUND1CNT_L => _channels[0].ReadControlL(), // Channel 1 Sweep register
        SOUND1CNT_H => _channels[0].ReadControlH(), // Channel 1 Duty/Length/Envelope
        SOUND1CNT_X => _channels[0].ReadControlX(), // Channel 1 Frequency/Control
        SOUND1CNT_X + 2 => 0, // Unused
        SOUND2CNT_L => _channels[1].ReadControlL(), // Channel 2 Duty/Length/Envelope
        SOUND2CNT_L + 2 => 0, // Unused
        SOUND2CNT_H => _channels[1].ReadControlH(), // Channel 2 Frequency/Control
        SOUND2CNT_H + 2 => 0, // Unused
        SOUND3CNT_L => _channels[2].ReadControlL(), // Channel 3 Stop/Wave RAM select
        SOUND3CNT_H => _channels[2].ReadControlH(), // Channel 3 Length/Volume
        SOUND3CNT_X => _channels[2].ReadControlX(), // Channel 3 Frequency/Control
        SOUND3CNT_X + 2 => 0, // Unused
        SOUND4CNT_L => _channels[3].ReadControlL(), // Channel 4 Length/Envelope
        SOUND4CNT_L + 2 => 0, // Unused
        SOUND4CNT_H => _channels[3].ReadControlH(), // Channel 4 Frequency/Control
        SOUND4CNT_H + 2 => 0, // Unused
        SOUNDCNT_L => _soundControlRegister.GetSoundCntL(), // Control Stereo/Volume/Enable
        SOUNDCNT_H => _soundControlRegister.GetSoundCntH(), // Control mixing/DMA control
        SOUNDCNT_X => SoundCntX(), // Control Sound on/off
        SOUNDCNT_X + 2 => 0, // Unused
        SOUNDBIAS => (ushort)((_biasLevel << 1) | (_samplingCycle << 14)), // Sound PWM Control
        SOUNDBIAS + 2 => 0, // Unused
        var _ when alignedAddress is >= WAVE_RAM and < WAVE_RAM + 16 => (_channels[2] as SoundChannel3)!.ReadWaveRam(alignedAddress), // Channel 3 wave pattern RAM
        _ => (ushort)openbus,
    };

    internal uint ReadWord(uint alignedAddress, uint openbus) => (uint)
        (ReadHalfWord(alignedAddress, openbus) | 
        (ReadHalfWord(alignedAddress + 2, openbus) << 16));

    internal void WriteByte(uint address, byte value)
    {
        throw new NotImplementedException("Byte write to APU registers not yet implemented");
    }

    internal void WriteHalfWord(uint alignedAddress, ushort value)
    {
        switch (alignedAddress)
        {
            case SOUND1CNT_L: // Channel 1 Sweep register
                _channels[0].WriteControlL(value);
                break;
            case SOUND1CNT_H: // Channel 1 Duty/Length/Envelope
                _channels[0].WriteControlH(value);
                break;
            case SOUND1CNT_X: // Channel 1 Frequency/Control
                _channels[0].WriteControlX(value);
                break;
            case SOUND2CNT_L: // Channel 2 Duty/Length/Envelope
                _channels[1].WriteControlL(value);
                break;
            case SOUND2CNT_H: // Channel 2 Frequency/Control
                _channels[1].WriteControlH(value);
                break;
            case SOUND3CNT_L: // Channel 3 Stop/Wave RAM select
                _channels[2].WriteControlL(value);
                break;
            case SOUND3CNT_H: // Channel 3 Length/Volume
                _channels[2].WriteControlH(value);
                break;
            case SOUND3CNT_X: // Channel 3 Frequency/Control
                _channels[2].WriteControlX(value);
                break;
            case SOUND4CNT_L: // Channel 4 Length/Envelope
                _channels[3].WriteControlL(value);
                break;
            case SOUND4CNT_H: // Channel 4 Frequency/Control
                _channels[3].WriteControlH(value);
                break;
            case SOUNDCNT_L: // Control Stereo/Volume/Enable
                _soundControlRegister.SetSoundCntL(value);
                break;
            case SOUNDCNT_H: // Control mixing/DMA control
                _soundControlRegister.SetSoundCntH(value);
                break;
            case SOUNDCNT_X: // Control Sound on/off
                _psgFifoMasterEnable = ((value >> 7) & 0b1) == 0b1;
                break;
            case SOUNDBIAS: // Sound PWM Control
                _biasLevel = (value >> 1) & 0b1_1111_1111;
                _samplingCycle = (value >> 14) & 0b11;
                break;
            case var _ when alignedAddress is >= WAVE_RAM and < WAVE_RAM + 16: // Channel 3 wave pattern RAM
                (_channels[2] as SoundChannel3)!.WriteWaveRam(alignedAddress, value);
                break;
            case FIFO_A: // Channel A FIFO low half word, Data 0-1
                // TODO
                break;
            case FIFO_A + 2: // Channel A FIFO high half word, Data 2-3
                // TODO
                break;
            case FIFO_B: // Channel B FIFO low half word, Data 0-1
                // TODO
                break;
            case FIFO_B + 2: // Channel B FIFO high half word, Data 2-3
                // TODO
                break;
        }
    }

    internal void WriteWord(uint alignedAddress, uint value)
    {
        WriteHalfWord(alignedAddress, (ushort)value);
        WriteHalfWord(alignedAddress + 2, (ushort)(value >> 16));
    }
}
