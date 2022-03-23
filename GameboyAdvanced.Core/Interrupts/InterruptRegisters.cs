using static GameboyAdvanced.Core.IORegs;

namespace GameboyAdvanced.Core.Interrupts;

/// <summary>
/// This class contains the raw data constituting the ARM interrupt registers.
/// 
/// It's really a part of the core ARM model instead of being a separate 
/// GBA component but is handled as one because the implementation are specific
/// to the GBA.
/// </summary>
public class InterruptRegisters
{
    private struct InterruptRegister
    {
        private bool _lcdVBlank;
        private bool _lcdHBlank;
        private bool _lcdVCounterMatch;
        private bool _timer0Overflow;
        private bool _timer1Overflow;
        private bool _timer2Overflow;
        private bool _timer3Overflow;
        private bool _serialComms;
        private bool _dma0;
        private bool _dma1;
        private bool _dma2;
        private bool _dma3;
        private bool _keypad;
        private bool _gamepak;

        internal ushort Get() =>
            (ushort)((_lcdVBlank ? (1u << 0) : 0u) |
            (_lcdHBlank ? (1u << 1) : 0u) |
            (_lcdVCounterMatch ? (1u << 2) : 0u) |
            (_timer0Overflow ? (1u << 3) : 0u) |
            (_timer1Overflow ? (1u << 4) : 0u) |
            (_timer2Overflow ? (1u << 5) : 0u) |
            (_timer3Overflow ? (1u << 6) : 0u) |
            (_serialComms ? (1u << 7) : 0u) |
            (_dma0 ? (1u << 8) : 0u) |
            (_dma1 ? (1u << 9) : 0u) |
            (_dma2 ? (1u << 10) : 0u) |
            (_dma3 ? (1u << 11) : 0u) |
            (_keypad ? (1u << 12) : 0u) |
            (_gamepak ? (1u << 13) : 0u));

        internal void SetB1(byte val)
        {
            _lcdVBlank = ((val & (1u << 0)) == 1u << 0);
            _lcdHBlank = ((val & (1u << 1)) == 1u << 1);
            _lcdVCounterMatch = ((val & (1u << 2)) == 1u << 2);
            _timer0Overflow = ((val & (1u << 3)) == 1u << 3);
            _timer1Overflow = ((val & (1u << 4)) == 1u << 4);
            _timer2Overflow = ((val & (1u << 5)) == 1u << 5);
            _timer3Overflow = ((val & (1u << 6)) == 1u << 6);
            _serialComms = ((val & (1u << 7)) == 1u << 7);
        }

        internal void SetB2(byte val)
        {
            _dma0 = ((val & (1u << 0)) == 1u << 8);
            _dma1 = ((val & (1u << 1)) == 1u << 9);
            _dma2 = ((val & (1u << 2)) == 1u << 10);
            _dma3 = ((val & (1u << 3)) == 1u << 11);
            _keypad = ((val & (1u << 4)) == 1u << 12);
            _gamepak = ((val & (1u << 5)) == 1u << 13);
        }

        internal void Set(Interrupt interrupt)
        {
            switch (interrupt)
            {
                case Interrupt.LCDVBlank:
                    _lcdVBlank = true;
                    break;
                case Interrupt.LCDHBlank:
                    _lcdHBlank = true;
                    break;
                case Interrupt.LCDVCounter:
                    _lcdVCounterMatch = true;
                    break;
                case Interrupt.Timer0Overflow:
                    _timer0Overflow = true;
                    break;
                case Interrupt.Timer1Overflow:
                    _timer1Overflow = true;
                    break;
                case Interrupt.Timer2Overflow:
                    _timer2Overflow = true;
                    break;
                case Interrupt.Timer3Overflow:
                    _timer3Overflow = true;
                    break;
                case Interrupt.SerialCommunication:
                    _serialComms = true;
                    break;
                case Interrupt.DMA0:
                    _dma0 = true;
                    break;
                case Interrupt.DMA1:
                    _dma1 = true;
                    break;
                case Interrupt.DMA2:
                    _dma2 = true;
                    break;
                case Interrupt.DMA3:
                    _dma3 = true;
                    break;
                case Interrupt.Keypad:
                    _keypad = true;
                    break;
                case Interrupt.GamePak:
                    _gamepak = true;
                    break;
            }
        }
    }

    private bool _interruptMasterEnable;
    private InterruptRegister _interruptEnable;
    private InterruptRegister _interruptRequest;
    internal bool CpuShouldIrq;
    internal bool ShouldBreakHalt;

    internal void Reset()
    {
        CpuShouldIrq = false;
        _interruptMasterEnable = false;
        _interruptEnable.Set(0);
        _interruptRequest.SetB1(0xFF);
        _interruptRequest.SetB2(0xFF);
    }

    internal void RaiseInterrupt(Interrupt interrupt)
    {
        var newVal = (ushort)(_interruptRequest.Get() | (ushort)interrupt);
        _interruptRequest.SetB1((byte)newVal);
        _interruptRequest.SetB2((byte)(newVal >> 8));
        UpdateCpuShouldIrq();
    }

    private void UpdateCpuShouldIrq()
    {
        CpuShouldIrq = _interruptMasterEnable && (_interruptEnable.Get() & _interruptRequest.Get()) != 0;
        ShouldBreakHalt = (_interruptEnable.Get() & _interruptRequest.Get()) != 0;
    }

    internal byte ReadByte(uint address) => (byte)(ReadHalfWord(address & 0xFFFF_FFFE) >> (int)(8 * (address & 1)));

    internal ushort ReadHalfWord(uint address) => address switch
    {
        IE => _interruptEnable.Get(),
        IF => _interruptRequest.Get(),
        IME => (ushort)(_interruptMasterEnable ? 1 : 0u),
        IME + 2 => 0, // TODO - Is this tested?
        _ => throw new Exception("Invalid read from interrupt registers")
    };

    internal uint ReadWord(uint address) =>
        (uint)(ReadHalfWord(address) | (ReadHalfWord(address + 2) << 16));

    internal void WriteByte(uint address, byte val)
    {
        switch (address)
        {
            case IE:
                _interruptEnable.SetB1(val);
                break;
            case IE + 1:
                _interruptEnable.SetB2(val);
                break;
            case IF:
                var currentB1 = (byte)(_interruptRequest.Get());
                var newValB1 = (byte)(currentB1 & ~val);
                _interruptRequest.SetB1(newValB1);
                break;
            case IF + 1:
                var currentB2 = (byte)(_interruptRequest.Get() >> 8);
                var newValB2 = (byte)(currentB2 & ~val);
                _interruptRequest.SetB1(newValB2);
                break;
            case IME:
                _interruptMasterEnable = (val & 0b1) == 0b1;
                break;
            case IME + 1:
                break;
            default:
                break;
        }

        UpdateCpuShouldIrq();
    }

    internal void WriteHalfWord(uint address, ushort val)
    {
        WriteByte(address, (byte)val);
        WriteByte(address + 1, (byte)(val >> 8));
    }

    internal void WriteWord(uint address, uint val)
    {
        WriteHalfWord(address, (ushort)val);
        WriteHalfWord(address + 2, (ushort)(val >> 16));
    }
}
