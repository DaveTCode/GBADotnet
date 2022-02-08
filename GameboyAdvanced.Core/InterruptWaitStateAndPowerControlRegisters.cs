using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyAdvanced.Core.Cpu.Interrupts;

/// <summary>
/// This class encapsulates the various IO registers which are located between
/// 0x0400_0200 and 0x0470_0000.
/// </summary>
internal class InterruptWaitStateAndPowerControlRegisters
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

        internal void Set(ushort val)
        {
            _lcdVBlank = (val & 0b1) == 0b1;
            _lcdHBlank = (val & 0b10) == 0b10;
            _lcdVCounterMatch = (val & 0b100) == 0b100;
            _timer0Overflow = (val & 0b1000) == 0b1000;
            _timer1Overflow = (val & 0b1_0000) == 0b1_0000;
            _timer2Overflow = (val & 0b10_0000) == 0b10_0000;
            _timer3Overflow = (val & 0b100_0000) == 0b100_0000;
            _serialComms = (val & 0b1000_0000) == 0b1000_0000;
            _dma0 = (val & 0b1_0000_0000) == 0b1_0000_0000;
            _dma1 = (val & 0b10_0000_0000) == 0b10_0000_0000;
            _dma2 = (val & 0b100_0000_0000) == 0b100_0000_0000;
            _dma3 = (val & 0b1000_0000_0000) == 0b1000_0000_0000;
            _keypad = (val & 0b1_0000_0000_0000) == 0b1_0000_0000_0000;
            _gamepak = (val & 0b10_0000_0000_0000) == 0b10_0000_0000_0000;
        }
    }

    private bool _interruptMasterEnable;
    private InterruptRegister _interruptEnable;
    private InterruptRegister _interruptRequest;

    internal (byte, int) ReadByte(uint _) => throw new NotImplementedException("Byte reads from interrupt registers not implemented");

    internal (ushort, int) ReadHalfWord(uint address) => address switch
    {
        0x0400_0200 => (_interruptEnable.Get(), 0),
        0x0400_0202 => (_interruptRequest.Get(), 0),
        0x0400_0208 => ((ushort)(_interruptMasterEnable ? 1 : 0u), 0),
        _ => throw new NotImplementedException($"Invalid address {address:X8} for interrupt registers")
    };

    internal (byte, int) ReadWord(uint _) => throw new NotImplementedException("Word reads from interrupt registers not implemented");

    internal int WriteByte(uint address, byte val)
    {
        var hwVal = (ushort)(val | (val << 8));
        var hwAddress = address & ~1u;
        return WriteHalfWord(hwAddress, hwVal); // TODO - Is this correct? That's how I think 16 bit address buses work when writing bytes to them
    }

    internal int WriteHalfWord(uint address, ushort val)
    {
        switch (address)
        {
            case 0x0400_0200: // IE - Interrupt Enable Register
                _interruptEnable.Set(val);
                return 0;
            case 0x0400_0202: // IE - Interrupt Request Flags / IRQ Acknowledge
                _interruptRequest.Set(val);
                return 0;
            case 0x0400_0208: // IME - Interrupt Master Enable Register
                _interruptMasterEnable = (val & 0b1) == 0b1;
                return 0;
            default:
                throw new NotImplementedException($"Address {address:X8} not implemented in IO registers");
        }
    }

    internal int WriteWord(uint address, uint val)
    {
        switch (address)
        {
            case 0x0400_0200: // IE/IF
                _interruptEnable.Set((ushort)val);
                _interruptRequest.Set((ushort)(val >> 8));
                return 0; // TODO - Do we add wait states for double writes on a 16 bit bus
            case 0x0400_0208: // IME
                _interruptMasterEnable = (val & 0b1) == 0b1;
                return 0;
            default:
                throw new NotImplementedException($"Address {address:X8} not implemented in IO registers");
        }
    }
}
