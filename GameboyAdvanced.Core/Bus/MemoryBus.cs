using static GameboyAdvanced.Core.IORegs;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Dma;
using GameboyAdvanced.Core.Input;
using GameboyAdvanced.Core.Rom;
using GameboyAdvanced.Core.Serial;
using GameboyAdvanced.Core.Timer;
using GameboyAdvanced.Core.Interrupts;

namespace GameboyAdvanced.Core.Bus;

/// <summary>
/// The memory bus is the interconnect unit between the CPU and the other components.
/// 
/// It is responsible for all the memory mapped requests and determining how 
/// many wait states should be injected.
/// </summary>
internal class MemoryBus
{
    private readonly BaseDebugger _debugger;
    private readonly Ppu.Ppu _ppu;
    private readonly Gamepad _gamepad;
    private readonly GamePak _gamePak;
    private readonly DmaDataUnit _dma;
    private readonly TimerController _timerController;
    private readonly InterruptRegisters _interruptRegisters;
    private readonly SerialController _serialController;
    private readonly byte[] _bios = new byte[0x4000];
    private readonly byte[] _onBoardWRam = new byte[0x4_0000];
    private readonly byte[] _onChipWRam = new byte[0x8000];
    private WaitControl _waitControl;
    private InternalMemoryControl _intMemoryControl;

    internal void Reset()
    {
        Array.Clear(_onBoardWRam);
        Array.Clear(_onChipWRam);
        _waitControl.Reset();
        _intMemoryControl.Reset();
    }

    internal MemoryBus(
        byte[] bios,
        Gamepad gamepad,
        GamePak gamePak,
        Ppu.Ppu ppu,
        DmaDataUnit dma,
        TimerController timerController,
        InterruptRegisters interruptRegisters,
        SerialController serialController,
        BaseDebugger debugger)
    {
        if (bios == null || bios.Length > _bios.Length) throw new ArgumentException($"Bios is invalid length {bios?.Length}", nameof(bios));
        Array.Fill<byte>(_bios, 0);
        Array.Copy(bios, 0, _bios, 0, Math.Min(_bios.Length, bios.Length));

        _ppu = ppu ?? throw new ArgumentNullException(nameof(ppu));
        _gamepad = gamepad ?? throw new ArgumentNullException(nameof(gamepad));
        _gamePak = gamePak ?? throw new ArgumentNullException(nameof(gamePak));
        _dma = dma ?? throw new ArgumentNullException(nameof(dma));
        _timerController = timerController ?? throw new ArgumentNullException(nameof(timerController));
        _interruptRegisters = interruptRegisters ?? throw new ArgumentNullException(nameof(interruptRegisters));
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _serialController = serialController ?? throw new ArgumentNullException(nameof(serialController));
        _waitControl = new WaitControl();
        _intMemoryControl = new InternalMemoryControl();
    }

    internal (byte, int) ReadByte(uint address, int seq)
    {
        var (val, waitStates) = address switch
        {
            uint _ when address <= 0x0000_3FFF => (_bios[address], 0), // TODO - Can only read from bios when IP is located in BIOS region
            uint _ when address is >= 0x0200_0000 and <= 0x02FF_FFFF => (_onBoardWRam[address & 0x3_FFFF], _intMemoryControl.WaitControlWRAM),
            uint _ when address is >= 0x0300_0000 and <= 0x03FF_FFFF => (_onChipWRam[address & 0x7FFF], 0),
            uint _ when address is >= 0x0400_0000 and <= 0x0400_03FE => address switch
            {
                uint _ when address is >= 0x0400_0000 and <= 0x0400_0056 => (_ppu.ReadRegisterByte(address), 0),
                uint _ when address is >= 0x0400_0060 and <= 0x0400_00A8 => ((byte)0, 0), // TODO - Sound registers
                uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE => (_dma.ReadByte(address), 0),
                uint _ when address is >= 0x0400_0100 and <= 0x0400_0110 => (_timerController.ReadByte(address), 0),
                uint _ when address is >= 0x0400_0120 and <= 0x0400_012C => (_serialController.ReadByte(address), 0),
                uint _ when address is >= 0x0400_0130 and <= 0x0400_0132 => (_gamepad.ReadByte(address), 0),
                uint _ when address is >= 0x0400_0134 and <= 0x0400_015A => (_serialController.ReadByte(address), 0),
                IE => (_interruptRegisters.ReadByte(address), 0),
                IF => (_interruptRegisters.ReadByte(address), 0),
                WAITCNT => ((byte)_waitControl.Get(), 0),
                IME => (_interruptRegisters.ReadByte(address), 0),
                POSTFLG => ((byte)1, 0), // TODO - Implement read/write of this during bios
                uint _ when (address & 0xFF00FFFF) == INTMEMCTRL => ((byte)_intMemoryControl.Get(), 0), // TODO - Do we just cast to byte here?
                _ => throw new ArgumentOutOfRangeException(nameof(address), $"IO registers at {address:X8} not mapped"),
            },
            uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF => (_ppu.ReadByte(address), 0),
            uint _ when address is >= 0x0800_0000 and <= 0x09FF_FFFF => (_gamePak.ReadByte(address & 0x1FF_FFFF), _waitControl.WaitState0[seq]),
            uint _ when address is >= 0x0A00_0000 and <= 0x0BFF_FFFF => (_gamePak.ReadByte(address & 0x1FF_FFFF), _waitControl.WaitState1[seq]),
            uint _ when address is >= 0x0C00_0000 and <= 0x0DFF_FFFF => (_gamePak.ReadByte(address & 0x1FF_FFFF), _waitControl.WaitState2[seq]),
            uint _ when address is >= 0x0E00_0000 and <= 0x0FFF_FFFF => (_gamePak.ReadSRam(address), _waitControl.SRAMWaitControl),
            _ => ((byte)0, 0), // TODO - Hacking for mgba test suite throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} not memory mapped")
        };

#if DEBUG
        _debugger.Log("R {0:X8}={1:X8}", address, val);
#endif
        return (val, waitStates);
    }

    internal (ushort, int) ReadHalfWord(uint unalignedAddress, int seq)
    {
        var alignedAddress = unalignedAddress & 0xFFFF_FFFE;

        var (val, waitStates) = alignedAddress switch
        {
            uint a when a <= 0x0000_3FFF => (Utils.ReadHalfWord(_bios, alignedAddress, 0x3FFF), 0), // TODO - Can only read from bios when IP is located in BIOS region
            uint a when a is >= 0x0200_0000 and <= 0x02FF_FFFF => (Utils.ReadHalfWord(_onBoardWRam, alignedAddress, 0x3_FFFF), _intMemoryControl.WaitControlWRAM),
            uint a when a is >= 0x0300_0000 and <= 0x03FF_FFFF => (Utils.ReadHalfWord(_onChipWRam, alignedAddress, 0x7FFF), 0),
            uint a when a is >= 0x0400_0000 and <= 0x0400_03FE => alignedAddress switch
            {
                uint _ when alignedAddress is >= 0x0400_0000 and <= 0x0400_0056 => (_ppu.ReadRegisterHalfWord(alignedAddress), 0),
                uint _ when alignedAddress is >= 0x0400_0060 and <= 0x0400_00A8 => ((ushort)0, 0), // TODO - Sound registers,
                uint _ when alignedAddress is >= 0x0400_00B0 and <= 0x0400_00DE => (_dma.ReadHalfWord(alignedAddress), 0),
                uint _ when alignedAddress is >= 0x0400_0100 and <= 0x0400_0110 => (_timerController.ReadHalfWord(alignedAddress), 0),
                uint _ when alignedAddress is >= 0x0400_0120 and <= 0x0400_012C => (_serialController.ReadHalfWord(alignedAddress), 0),
                uint _ when alignedAddress is >= 0x0400_0130 and <= 0x0400_0132 => (_gamepad.ReadHalfWord(alignedAddress), 0),
                uint _ when alignedAddress is >= 0x0400_0134 and <= 0x0400_015A => (_serialController.ReadHalfWord(alignedAddress), 0),
                IE => (_interruptRegisters.ReadHalfWord(alignedAddress), 0),
                IF => (_interruptRegisters.ReadHalfWord(alignedAddress), 0),
                WAITCNT => (_waitControl.Get(), 0),
                IME => (_interruptRegisters.ReadHalfWord(alignedAddress), 0),
                POSTFLG => ((ushort)1, 0), // TODO - Implement read/write of this during bios
                uint _ when (alignedAddress & 0xFF00FFFF) == INTMEMCTRL => ((ushort)_intMemoryControl.Get(), 0), // TODO - Do we just cast to ushort here?
                _ => throw new ArgumentOutOfRangeException(nameof(alignedAddress), $"IO registers at {alignedAddress:X8} not mapped"),
            },
            uint a when a is >= 0x0500_0000 and <= 0x07FF_FFFF => (_ppu.ReadHalfWord(alignedAddress), 0),
            uint _ when alignedAddress is >= 0x0800_0000 and <= 0x09FF_FFFF => (_gamePak.ReadHalfWord(alignedAddress & 0x1FF_FFFF), _waitControl.WaitState0[seq]),
            uint _ when alignedAddress is >= 0x0A00_0000 and <= 0x0BFF_FFFF => (_gamePak.ReadHalfWord(alignedAddress & 0x1FF_FFFF), _waitControl.WaitState1[seq]),
            uint _ when alignedAddress is >= 0x0C00_0000 and <= 0x0DFF_FFFF => (_gamePak.ReadHalfWord(alignedAddress & 0x1FF_FFFF), _waitControl.WaitState2[seq]),
            uint _ when alignedAddress is >= 0x0E00_0000 and <= 0x0FFF_FFFF => ((ushort)(_gamePak.ReadSRam(unalignedAddress) * 0x0101), _waitControl.SRAMWaitControl),
            _ => ((ushort)0, 0), // TODO - Hacking for mgba test suite throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} not memory mapped")
        };

#if DEBUG
        _debugger.Log("R {0:X8}={1:X8}", address, val);
#endif
        return (val, waitStates);
    }

    internal (uint, int) ReadWord(uint unalignedAddress, int seq)
    {
        var alignedAddress = unalignedAddress & 0xFFFF_FFFC;

        var (val, waitStates) = alignedAddress switch
        {
            uint a when a <= 0x0000_3FFF => (Utils.ReadWord(_bios, alignedAddress, 0x3FFF), 0), // TODO - Can only read from bios when IP is located in BIOS region
            uint a when a is >= 0x0200_0000 and <= 0x02FF_FFFF => (Utils.ReadWord(_onBoardWRam, alignedAddress, 0x3_FFFF), _intMemoryControl.WaitControlWRAM * 2),
            uint a when a is >= 0x0300_0000 and <= 0x03FF_FFFF => (Utils.ReadWord(_onChipWRam, alignedAddress, 0x7FFF), 0),
            uint a when a is >= 0x0400_0000 and <= 0x0400_03FE => alignedAddress switch
            {
                uint _ when alignedAddress is >= 0x0400_0000 and <= 0x0400_0056 => ((uint)(_ppu.ReadRegisterHalfWord(alignedAddress) | (_ppu.ReadRegisterHalfWord(alignedAddress + 2) << 16)), 0),
                uint _ when alignedAddress is >= 0x0400_0060 and <= 0x0400_00A8 => throw new NotImplementedException("Sound registers not yet implemented"),
                uint _ when alignedAddress is >= 0x0400_00B0 and <= 0x0400_00DE => (_dma.ReadWord(alignedAddress), 0),
                uint _ when alignedAddress is >= 0x0400_0100 and <= 0x0400_0110 => (_timerController.ReadWord(alignedAddress), 0),
                uint _ when alignedAddress is >= 0x0400_0120 and <= 0x0400_012C => (_serialController.ReadWord(alignedAddress), 0),
                uint _ when alignedAddress is >= 0x0400_0130 and <= 0x0400_0132 => ((uint)(_gamepad.ReadHalfWord(alignedAddress) | (_gamepad.ReadHalfWord(alignedAddress + 2) << 16)), 0),
                uint _ when alignedAddress is >= 0x0400_0134 and <= 0x0400_015A => (_serialController.ReadWord(alignedAddress), 0),
                IE => (_interruptRegisters.ReadWord(alignedAddress), 0),
                WAITCNT => (_waitControl.Get(), 0),
                IME => (_interruptRegisters.ReadWord(alignedAddress), 0),
                POSTFLG => (1, 0), // TODO - Implement read/write of this during bios
                uint _ when (alignedAddress & 0xFF00FFFF) == INTMEMCTRL => (_intMemoryControl.Get(), 0),
                _ => throw new ArgumentOutOfRangeException(nameof(alignedAddress), $"IO registers at {alignedAddress:X8} not mapped"),
            },
            uint a when a is >= 0x0500_0000 and <= 0x07FF_FFFF => ((uint)(_ppu.ReadHalfWord(alignedAddress) | (_ppu.ReadHalfWord(alignedAddress + 2) << 16)), 1),
            uint _ when alignedAddress is >= 0x0800_0000 and <= 0x09FF_FFFF => (_gamePak.ReadWord(alignedAddress & 0x1FF_FFFF), _waitControl.WaitState0[seq] + _waitControl.WaitState0[1]),
            uint _ when alignedAddress is >= 0x0A00_0000 and <= 0x0BFF_FFFF => (_gamePak.ReadWord(alignedAddress & 0x1FF_FFFF), _waitControl.WaitState1[seq] + _waitControl.WaitState1[1]),
            uint _ when alignedAddress is >= 0x0C00_0000 and <= 0x0DFF_FFFF => (_gamePak.ReadWord(alignedAddress & 0x1FF_FFFF), _waitControl.WaitState2[seq] + _waitControl.WaitState2[1]),
            uint _ when alignedAddress is >= 0x0E00_0000 and <= 0x0FFF_FFFF => (_gamePak.ReadSRam(unalignedAddress) * 0x01010101u, _waitControl.SRAMWaitControl),
            _ => (0u, 0), // TODO - Hacking for mgba test suite throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} not memory mapped")
        };

#if DEBUG
        _debugger.Log("R {0:X8}={1:X8}", address, val);
#endif
        return (val, waitStates);
    }

    internal int WriteByte(uint address, byte value, int seq)
    {
#if DEBUG
        _debugger.Log("W {0:X8}={1:X8}", address, value);
#endif
        switch (address)
        {
            case uint _ when address <= 0x0000_3FFF:
                return 0;
            case uint _ when address is >= 0x0200_0000 and <= 0x02FF_FFFF:
                _onBoardWRam[address & 0x3_FFFF] = value;
                return _intMemoryControl.WaitControlWRAM;
            case uint _ when address is >= 0x0300_0000 and <= 0x03FF_FFFF:
                _onChipWRam[address & 0x7FFF] = value;
                return 0;
            case uint _ when address is >= 0x0400_0000 and <= 0x0400_03FE:
                switch (address)
                {
                    case uint _ when address is >= 0x0400_0000 and <= 0x0400_0056:
                        _ppu.WriteRegisterByte(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0060 and <= 0x0400_00A8:
                        // TODO - No APU or sound registers yet
                        return 0;
                    case uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE:
                        _dma.WriteByte(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0100 and <= 0x0400_0110:
                        _timerController.WriteByte(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0120 and <= 0x0400_012C:
                        _serialController.WriteByte(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0130 and <= 0x0400_0132:
                        _gamepad.WriteByte(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0134 and <= 0x0400_015A:
                        _serialController.WriteByte(address, value);
                        return 0;
                    case WAITCNT:
                        _waitControl.Set(value); // TODO - Setting byte value into waitcontrol
                        return 0;
                    case IME:
                    case IE:
                    case IF:
                        _interruptRegisters.WriteByte(address, value);
                        return 0;
                    case POSTFLG:
                        return 0; // TODO - Handle writing to POSTFLG during BIOS
                    case HALTCNT:
                        return 0; // TODO - Ignoring writes to HALTCNT as it's only so far used in mgba suite
                    case UNDOCUMENTED_410: // "The BIOS writes the 8bit value 0FFh to this address. Purpose Unknown." - No$ GbaTek
                        return 0;
                    case uint _ when (address & 0xFF00FFFF) == INTMEMCTRL:
                        throw new NotImplementedException("Can't set int memory control as byte or wait control for WRAM will get locked up at 15");
                    default:
                        throw new ArgumentOutOfRangeException(nameof(address), $"IO registers at address {address:X8} not mapped");
                };
            case uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF:
                _ppu.WriteByte(address, value);
                return 0;
            case uint _ when address is >= 0x0800_0000 and <= 0x0DFF_FFFF:
                return 0; // TODO - Is it right that no wait states occur on attempted writes to Gamepak?
            case uint _ when address is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                _gamePak.WriteSRam(address, value);
                return _waitControl.SRAMWaitControl;
            default:
                return 0;
                // TODO - throw new ArgumentOutOfRangeException(nameof(address));
        }
    }

    internal int WriteHalfWord(uint unalignedAddress, ushort value, int seq)
    {
        var alignedAddress = unalignedAddress & 0xFFFF_FFFE;
#if DEBUG
        _debugger.Log("W {0:X8}={1:X8}", address, value);
#endif
        switch (alignedAddress)
        {
            case uint _ when alignedAddress <= 0x0000_3FFF:
                return 0;
            case uint _ when alignedAddress is >= 0x0200_0000 and <= 0x02FF_FFFF:
                Utils.WriteHalfWord(_onBoardWRam, 0x3_FFFF, alignedAddress, value);
                return _intMemoryControl.WaitControlWRAM;
            case uint _ when alignedAddress is >= 0x0300_0000 and <= 0x03FF_FFFF:
                Utils.WriteHalfWord(_onChipWRam, 0x7FFF, alignedAddress, value);
                return 0;
            case uint _ when alignedAddress is >= 0x0400_0000 and <= 0x0400_03FE:
                switch (alignedAddress)
                {
                    case uint _ when alignedAddress is >= 0x0400_0000 and <= 0x0400_0056:
                        _ppu.WriteRegisterHalfWord(alignedAddress, value);
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_0060 and <= 0x0400_00A8:
                        // TODO - No APU or sound registers yet
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_00B0 and <= 0x0400_00DE:
                        _dma.WriteHalfWord(alignedAddress, value);
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_0100 and <= 0x0400_0110:
                        _timerController.WriteHalfWord(alignedAddress, value);
                        return 0;
                    case 0x0400_0114: // BIOS bug writes to this
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_0120 and <= 0x0400_012C:
                        _serialController.WriteHalfWord(alignedAddress, value);
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_0130 and <= 0x0400_0132:
                        _gamepad.WriteHalfWord(alignedAddress, value);
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_0134 and <= 0x0400_015A:
                        _serialController.WriteHalfWord(alignedAddress, value);
                        return 0;
                    case POSTFLG:
                        return 0; // TODO - Handle writing to POSTFLG during BIOS
                    case WAITCNT:
                        _waitControl.Set(value);
                        return 0;
                    case IME:
                    case IE:
                    case IF:
                        _interruptRegisters.WriteHalfWord(alignedAddress, value);
                        return 0;
                    case uint _ when (alignedAddress & 0xFF00FFFF) == INTMEMCTRL:
                        throw new NotImplementedException("Can't set int memory control as ushort or wait control for WRAM will get locked up at 15");
                    default:
                        throw new ArgumentOutOfRangeException(nameof(alignedAddress), $"IO registers at address {alignedAddress:X8} not mapped");
                };
            case uint _ when alignedAddress is >= 0x0500_0000 and <= 0x07FF_FFFF:
                _ppu.WriteHalfWord(alignedAddress, value);
                return 0;
            case uint _ when alignedAddress is >= 0x0800_0000 and <= 0x0DFF_FFFF:
                return 0; // TODO - Is it right that no wait states occur on attempted writes to Gamepak?
            case uint _ when alignedAddress is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                var shift = (ushort)(unalignedAddress & 1) << 3;
                _gamePak.WriteSRam(unalignedAddress, (byte)(value >> shift));
                return _waitControl.SRAMWaitControl;
            default:
                return 0; // TODO - Just hacking this in for now because mgba test suite writes to 0x04FFF780 during startup
                // throw new ArgumentOutOfRangeException(nameof(address));
        }
    }

    internal int WriteWord(uint unalignedAddress, uint value, int seq)
    {
        var alignedAddress = unalignedAddress & 0xFFFF_FFFC;
#if DEBUG
        _debugger.Log("W {0:X8}={1:X8}", address, value);
#endif

        switch (alignedAddress)
        {
            case uint _ when alignedAddress <= 0x0000_3FFF:
                return 0;
            case uint _ when alignedAddress is >= 0x0200_0000 and <= 0x02FF_FFFF:
                Utils.WriteWord(_onBoardWRam, 0x3_FFFF, alignedAddress, value);
                return _intMemoryControl.WaitControlWRAM * 2;
            case uint _ when alignedAddress is >= 0x0300_0000 and <= 0x03FF_FFFF:
                Utils.WriteWord(_onChipWRam, 0x7FFF, alignedAddress, value);
                return 0;
            case uint _ when alignedAddress is >= 0x0400_0000 and <= 0x0400_03FE:
                switch (alignedAddress)
                {
                    case uint _ when alignedAddress is >= 0x0400_0000 and <= 0x0400_0056:
                        _ppu.WriteRegisterHalfWord(alignedAddress, (ushort)value);
                        _ppu.WriteRegisterHalfWord(alignedAddress + 2, (ushort)(value >> 16));
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_0060 and <= 0x0400_00A8:
                        // TODO - No APU or sound registers yet
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_00B0 and <= 0x0400_00DE:
                        _dma.WriteWord(alignedAddress, value);
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_0100 and <= 0x0400_0110:
                        _timerController.WriteWord(alignedAddress, value);
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_0120 and <= 0x0400_012C:
                        _serialController.WriteWord(alignedAddress, value);
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_0130 and <= 0x0400_0132:
                        _gamepad.WriteHalfWord(alignedAddress, (ushort)value);
                        _gamepad.WriteHalfWord(alignedAddress + 2, (ushort)(value >> 16));
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_0134 and <= 0x0400_015A:
                        _serialController.WriteWord(alignedAddress, value);
                        return 0;
                    case POSTFLG:
                        return 0; // TODO - Handle writing to POSTFLG during BIOS
                    case WAITCNT:
                        _waitControl.Set((ushort)value);
                        // 206 is unused so no extra write here
                        return 0;
                    case IME:
                    case IE:
                    case IF:
                        _interruptRegisters.WriteWord(alignedAddress, value);
                        return 0;
                    case uint _ when (alignedAddress & 0xFF00FFFF) == INTMEMCTRL:
                        _intMemoryControl.Set(value);
                        return 0;
                    default:
                        return 0;
                        throw new ArgumentOutOfRangeException(nameof(alignedAddress), $"IO registers at address {alignedAddress:X8} not mapped");
                };
            case uint _ when alignedAddress is >= 0x0500_0000 and <= 0x07FF_FFFF:
                _ppu.WriteHalfWord(alignedAddress, (ushort)value);
                _ppu.WriteHalfWord(alignedAddress + 2, (ushort)(value >> 16));
                return 1;
            case uint _ when alignedAddress is >= 0x0800_0000 and <= 0x0DFF_FFFF:
                return 0; // TODO - Is it right that no wait states occur on attempted writes to Gamepak?
            case uint _ when alignedAddress is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                var shift = (int)(unalignedAddress & 0b11) << 3;
                _gamePak.WriteSRam(unalignedAddress, (byte)(value >> shift));
                return _waitControl.SRAMWaitControl;
            default:
                return 0; // TODO - Just hacking this in for now because mgba test suite writes to 0x04FFF780 during startup
                // throw new ArgumentOutOfRangeException(nameof(unalignedAddress));
        }
    }
}