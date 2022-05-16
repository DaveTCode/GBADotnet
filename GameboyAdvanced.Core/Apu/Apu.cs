using GameboyAdvanced.Core.Apu.Channels;
using GameboyAdvanced.Core.Debug;
using static GameboyAdvanced.Core.IORegs;

namespace GameboyAdvanced.Core.Apu;

public unsafe class Apu
{
    // 16777216 (master clock speed) / 65536 (desired sample rate) - GBA internally uses 65536 resampling but that's unnecessarily fast
    internal const int CyclesPerSample = 256;

    /// <summary>
    /// The internal sample buffer is where we store combined samples before 
    /// sending them to the external audio system. Increasing this will add 
    /// lag to audio but improve performance as fewer syscalls are needed.
    /// 
    /// Note that two shorts are added to the buffer at a time (left, right)
    /// </summary>
    private readonly byte[] InternalSampleBuffer = new byte[256 * sizeof(short)]; 
    private int InternalSampleBufferPtr = 0;

    private readonly Device _device;
    public readonly BaseDebugger _debugger;

    public readonly DmaChannel[] _dmaChannels;
    public readonly GBSoundChannel[] _channels = new GBSoundChannel[4]
    {
        new SoundChannel1(), 
        new SoundChannel2(), 
        new SoundChannel3(),
        new SoundChannel4(),
    };
    public readonly SoundControlRegister _soundControlRegister;
    public bool _psgFifoMasterEnable;
    public int _biasLevel;
    public int _samplingCycle;

    public Apu(Device device, BaseDebugger debugger)
    {
        _device = device;
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _dmaChannels = new DmaChannel[2]
        {
            new DmaChannel(1, device), new DmaChannel(2, device),
        };
        _soundControlRegister = new SoundControlRegister(_dmaChannels);
        Reset();
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
        Array.Clear(InternalSampleBuffer);
        InternalSampleBufferPtr = 0;
    }

    /// <summary>
    /// The sample event is the scheduler based event where samples are taken 
    /// from the various internal audio subsystems and placed on into a buffer
    /// to then be sent on to the external audio system.
    /// </summary>
    internal static void SampleEvent(Device device)
    {
        var apu = device.Apu;

        short left = 0;
        short right = 0;

        if (apu._psgFifoMasterEnable)
        {
            // TODO - Handle GB audio channels

            // Add DMA based channels to the overall sample - these generate their
            // samples asynchronously through scheduler events based on the timer
            for (var ii = 0; ii < 2; ii ++)
            {
                var channel = apu._dmaChannels[ii];
                if (channel.EnableLeft) left += channel.CurrentValue;
                if (channel.EnableRight) right += channel.CurrentValue;
            }
        }

        left *= 64;
        right *= 64;
        apu.InternalSampleBuffer[apu.InternalSampleBufferPtr] = (byte)left;
        apu.InternalSampleBuffer[apu.InternalSampleBufferPtr + 1] = (byte)(left >> 8);
        apu.InternalSampleBuffer[apu.InternalSampleBufferPtr + 2] = (byte)right;
        apu.InternalSampleBuffer[apu.InternalSampleBufferPtr + 3] = (byte)(right >> 8);

        apu.InternalSampleBufferPtr += 4;
        if (apu.InternalSampleBufferPtr == apu.InternalSampleBuffer.Length)
        {
            apu.InternalSampleBufferPtr = 0;

            device.SendAudioSamples(apu.InternalSampleBuffer);
        }

        device.Scheduler.ScheduleEvent(EventType.ApuSample, &SampleEvent, CyclesPerSample);
    }
    
    private ushort SoundCntX()
    {
        // TODO - Return whether sounds are currently playing as well
        return (ushort)(_psgFifoMasterEnable ? (1 << 7) : 0);
    }

    internal void OverflowTimer(bool isTimer1)
    {
        for (var ii = 0; ii < 2; ii++)
        {
            var channel = _dmaChannels[ii];
            if ((channel.SelectTimer1 && isTimer1) || (!channel.SelectTimer1 && !isTimer1))
            {
                channel.StepFifo();
            }
        }
    }

    internal byte ReadByte(uint address, uint openbus) => 
        (byte)(ReadHalfWord(address, openbus) >> (int)(8 * (address & 1)));

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
        switch (address & 0xFFFF_FFFE)
        {
            case SOUND1CNT_L: // Channel 1 Sweep register
                _channels[0].WriteControlL(value, address & 1);
                break;
            case SOUND1CNT_H: // Channel 1 Duty/Length/Envelope
                _channels[0].WriteControlH(value, address & 1);
                break;
            case SOUND1CNT_X: // Channel 1 Frequency/Control
                _channels[0].WriteControlX(value, address & 1);
                break;
            case SOUND2CNT_L: // Channel 2 Duty/Length/Envelope
                _channels[1].WriteControlL(value, address & 1);
                break;
            case SOUND2CNT_H: // Channel 2 Frequency/Control
                _channels[1].WriteControlH(value, address & 1);
                break;
            case SOUND3CNT_L: // Channel 3 Stop/Wave RAM select
                _channels[2].WriteControlL(value, address & 1);
                break;
            case SOUND3CNT_H: // Channel 3 Length/Volume
                _channels[2].WriteControlH(value, address & 1);
                break;
            case SOUND3CNT_X: // Channel 3 Frequency/Control
                _channels[2].WriteControlX(value, address & 1);
                break;
            case SOUND4CNT_L: // Channel 4 Length/Envelope
                _channels[3].WriteControlL(value, address & 1);
                break;
            case SOUND4CNT_H: // Channel 4 Frequency/Control
                _channels[3].WriteControlH(value, address & 1);
                break;
            case SOUNDCNT_L: // Control Stereo/Volume/Enable
                _soundControlRegister.SetSoundCntL(value, address & 1);
                break;
            case SOUNDCNT_H: // Control mixing/DMA control
                _soundControlRegister.SetSoundCntH(value, address & 1);
                break;
            case SOUNDCNT_X: // Control Sound on/off
                if ((address & 1) == 0)
                {
                    _psgFifoMasterEnable = ((value >> 7) & 0b1) == 0b1;
                }
                break;
            case SOUNDBIAS: // Sound PWM Control
                if ((address & 1) == 0)
                {
                    _biasLevel = (_biasLevel & 0b1_0000_0000) | (value >> 1);
                }
                else
                {
                    _biasLevel = (_biasLevel & 0b0_0111_1111) | ((value & 0b11) << 7);
                    _samplingCycle = (value >> 6) & 0b11;
                }
                break;
            case var _ when address is >= WAVE_RAM and < WAVE_RAM + 16: // Channel 3 wave pattern RAM
                (_channels[2] as SoundChannel3)!.WriteWaveRamByte(address, value);
                break;
            case FIFO_A:
            case FIFO_A + 2:
                _dmaChannels[0].InsertSampleByte(value);
                break;
            case FIFO_B:
            case FIFO_B + 2:
                _dmaChannels[1].InsertSampleByte(value);
                break;
        }
    }

    internal void WriteHalfWord(uint alignedAddress, ushort value)
    {
        WriteByte(alignedAddress, (byte)value);
        WriteByte(alignedAddress + 1, (byte)(value >> 8));
    }

    internal void WriteWord(uint alignedAddress, uint value)
    {
        WriteHalfWord(alignedAddress, (ushort)value);
        WriteHalfWord(alignedAddress + 2, (ushort)(value >> 16));
    }
}
