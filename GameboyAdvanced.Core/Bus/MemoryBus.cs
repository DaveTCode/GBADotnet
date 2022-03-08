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
    private readonly Bios _bios;
    private readonly byte[] _onBoardWRam = new byte[0x4_0000];
    private readonly byte[] _onChipWRam = new byte[0x8000];
    private WaitControl _waitControl;
    private InternalMemoryControl _intMemoryControl;

    internal void Reset(bool skipBios)
    {
        Array.Clear(_onBoardWRam);
        Array.Clear(_onChipWRam);
        _waitControl.Reset();
        _intMemoryControl.Reset();
        _bios.Reset(skipBios);
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
        BaseDebugger debugger,
        bool skipBios)
    {
        _bios = new Bios(bios, skipBios);
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

    internal byte ReadByte(uint address, int seq, uint r15, uint D, ref int waitStates)
    {
        switch (address)
        {
            case uint _ when address <= 0x0000_3FFF:
                return _bios.ReadByte(address, r15);
            case uint _ when address is >= 0x0200_0000 and <= 0x02FF_FFFF:
                waitStates += _intMemoryControl.WaitControlWRAM;
                return _onBoardWRam[address & 0x3_FFFF];
            case uint _ when address is >= 0x0300_0000 and <= 0x03FF_FFFF:
                return _onChipWRam[address & 0x7FFF];
            case uint _ when address is >= 0x0400_0000 and <= 0x0400_03FE:
                return address switch
                {
                    uint _ when address is >= 0x0400_0000 and <= 0x0400_0056 => _ppu.ReadRegisterByte(address, D),
                    uint _ when address is >= 0x0400_0060 and <= 0x0400_00A8 => (byte)0, // TODO - Sound registers
                    uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE => _dma.ReadByte(address),
                    uint _ when address is >= 0x0400_0100 and <= 0x0400_0109 => _timerController.ReadByte(address),
                    uint _ when address is >= 0x0400_0120 and <= 0x0400_012C => _serialController.ReadByte(address),
                    uint _ when address is >= 0x0400_0130 and <= 0x0400_0132 => _gamepad.ReadByte(address),
                    uint _ when address is >= 0x0400_0134 and <= 0x0400_015A => _serialController.ReadByte(address),
                    IE => _interruptRegisters.ReadByte(address),
                    IF => _interruptRegisters.ReadByte(address),
                    WAITCNT => (byte)_waitControl.Get(),
                    IME => _interruptRegisters.ReadByte(address),
                    POSTFLG => (byte)1, // TODO - Implement read/write of this during bios
                    uint _ when (address & 0xFF00FFFF) == INTMEMCTRL => (byte)_intMemoryControl.Get(), // TODO - Do we just cast to byte here?
                    _ => (byte)D, // Open bus
                };
            case uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF:
                return _ppu.ReadByte(address);
            case uint _ when address is >= 0x0800_0000 and <= 0x09FF_FFFF:
                waitStates += _waitControl.WaitState0[seq];
                return _gamePak.ReadByte(address & 0x1FF_FFFF);
            case uint _ when address is >= 0x0A00_0000 and <= 0x0BFF_FFFF:
                waitStates += _waitControl.WaitState1[seq];
                return _gamePak.ReadByte(address & 0x1FF_FFFF);
            case uint _ when address is >= 0x0C00_0000 and <= 0x0DFF_FFFF: 
                waitStates += _waitControl.WaitState2[seq];
                return _gamePak.ReadByte(address & 0x1FF_FFFF);
            case uint _ when address is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                waitStates += _waitControl.SRAMWaitControl;
                return _gamePak.ReadSRam(address);
            default:
                var rotate = (address & 0b11) * 8;
                return (byte)(D >> (int)rotate);
        };
    }

    internal ushort ReadHalfWord(uint unalignedAddress, int seq, uint r15, uint D, ref int waitStates)
    {
        var alignedAddress = unalignedAddress & 0xFFFF_FFFE;

        switch (alignedAddress)
        {
            case uint _ when alignedAddress <= 0x0000_3FFF:
                return _bios.ReadHalfWord(unalignedAddress, r15);
            case uint _ when alignedAddress is >= 0x0200_0000 and <= 0x02FF_FFFF:
                waitStates += _intMemoryControl.WaitControlWRAM;
                return Utils.ReadHalfWord(_onBoardWRam, alignedAddress, 0x3_FFFF);
            case uint a when a is >= 0x0300_0000 and <= 0x03FF_FFFF:
                return Utils.ReadHalfWord(_onChipWRam, alignedAddress, 0x7FFF);
            case uint a when a is >= 0x0400_0000 and <= 0x0400_03FE:
                return alignedAddress switch
                {
                    uint _ when alignedAddress is >= 0x0400_0000 and <= 0x0400_0056 => _ppu.ReadRegisterHalfWord(alignedAddress, D),
                    uint _ when alignedAddress is >= 0x0400_0060 and <= 0x0400_00A8 => (ushort)0, // TODO - Sound registers,
                    uint _ when alignedAddress is >= 0x0400_00B0 and <= 0x0400_00DE => _dma.ReadHalfWord(alignedAddress, D),
                    uint _ when alignedAddress is >= 0x0400_0100 and <= 0x0400_0108 => _timerController.ReadHalfWord(alignedAddress),
                    uint _ when alignedAddress is >= 0x0400_0120 and <= 0x0400_012C => _serialController.ReadHalfWord(alignedAddress),
                    uint _ when alignedAddress is >= 0x0400_0130 and <= 0x0400_0132 => _gamepad.ReadHalfWord(alignedAddress),
                    uint _ when alignedAddress is >= 0x0400_0134 and <= 0x0400_015A => _serialController.ReadHalfWord(alignedAddress),
                    IE => _interruptRegisters.ReadHalfWord(alignedAddress),
                    IF => _interruptRegisters.ReadHalfWord(alignedAddress),
                    WAITCNT => _waitControl.Get(),
                    IME => _interruptRegisters.ReadHalfWord(alignedAddress),
                    POSTFLG => (ushort)1, // TODO - Implement read/write of this during bios
                    uint _ when (alignedAddress & 0xFF00FFFF) == INTMEMCTRL => (ushort)_intMemoryControl.Get(), // TODO - Do we just cast to ushort here?
                    _ => (ushort)D, // Open bus
                };
            case uint _ when alignedAddress is >= 0x0500_0000 and <= 0x07FF_FFFF: 
                return _ppu.ReadHalfWord(alignedAddress);
            case uint _ when alignedAddress is >= 0x0800_0000 and <= 0x09FF_FFFF:
                waitStates += _waitControl.WaitState0[seq];
                return _gamePak.ReadHalfWord(alignedAddress & 0x1FF_FFFF);
            case uint _ when alignedAddress is >= 0x0A00_0000 and <= 0x0BFF_FFFF:
                waitStates += _waitControl.WaitState1[seq];
                return _gamePak.ReadHalfWord(alignedAddress & 0x1FF_FFFF);
            case uint _ when alignedAddress is >= 0x0C00_0000 and <= 0x0DFF_FFFF:
                waitStates += _waitControl.WaitState2[seq];
                return _gamePak.ReadHalfWord(alignedAddress & 0x1FF_FFFF);
            case uint _ when alignedAddress is >= 0x0E00_0000 and <= 0x0FFF_FFFF: 
                waitStates += _waitControl.SRAMWaitControl;
                return (ushort)(_gamePak.ReadSRam(unalignedAddress) * 0x0101);
            default:
                // Open bus
                if ((unalignedAddress & 0b11) > 1)
                {
                    return (ushort)((D >> 16));
                }

                return (ushort)D;
        };
    }

    internal uint ReadWord(uint unalignedAddress, int seq, uint r15, uint D, ref int waitStates)
    {
        var alignedAddress = unalignedAddress & 0xFFFF_FFFC;

        switch (alignedAddress)
        {
            case uint _ when alignedAddress <= 0x0000_3FFF:
                return _bios.ReadWord(unalignedAddress, r15);
            case uint _ when alignedAddress is >= 0x0200_0000 and <= 0x02FF_FFFF:
                waitStates += _intMemoryControl.WaitControlWRAM * 2;
                return Utils.ReadWord(_onBoardWRam, alignedAddress, 0x3_FFFF);
            case uint _ when alignedAddress is >= 0x0300_0000 and <= 0x03FF_FFFF:
                return Utils.ReadWord(_onChipWRam, alignedAddress, 0x7FFF);
            case uint _ when alignedAddress is >= 0x0400_0000 and <= 0x0400_03FE:
                return alignedAddress switch
                {
                    uint _ when alignedAddress is >= 0x0400_0000 and <= 0x0400_0056 => (uint)(_ppu.ReadRegisterHalfWord(alignedAddress, D) | (_ppu.ReadRegisterHalfWord(alignedAddress + 2, D) << 16)),
                    uint _ when alignedAddress is >= 0x0400_0060 and <= 0x0400_00A8 => 0, // TODO - Sound registers not implemented
                    uint _ when alignedAddress is >= 0x0400_00B0 and <= 0x0400_00DE => _dma.ReadWord(alignedAddress, D),
                    uint _ when alignedAddress is >= 0x0400_0100 and <= 0x0400_0108 => _timerController.ReadWord(alignedAddress),
                    uint _ when alignedAddress is >= 0x0400_0120 and <= 0x0400_012C => _serialController.ReadWord(alignedAddress),
                    uint _ when alignedAddress is >= 0x0400_0130 and <= 0x0400_0132 => (uint)(_gamepad.ReadHalfWord(alignedAddress) | (_gamepad.ReadHalfWord(alignedAddress + 2) << 16)),
                    uint _ when alignedAddress is >= 0x0400_0134 and <= 0x0400_015A => _serialController.ReadWord(alignedAddress),
                    IE => _interruptRegisters.ReadWord(alignedAddress),
                    WAITCNT => _waitControl.Get(),
                    IME => _interruptRegisters.ReadWord(alignedAddress),
                    POSTFLG => 1, // TODO - Implement read/write of this during bios
                    uint _ when (alignedAddress & 0xFF00FFFF) == INTMEMCTRL => _intMemoryControl.Get(),
                    _ => D, // Open bus
                };
            case uint a when a is >= 0x0500_0000 and <= 0x07FF_FFFF:
                waitStates += 1; // TODO - Adding a single wait state here because it's 2 memory accesses over PPU 16 bit bus
                return (uint)(_ppu.ReadHalfWord(alignedAddress) | (_ppu.ReadHalfWord(alignedAddress + 2) << 16));
            case uint _ when alignedAddress is >= 0x0800_0000 and <= 0x09FF_FFFF:
                waitStates += _waitControl.WaitState0[seq] + _waitControl.WaitState0[1];
                return _gamePak.ReadWord(alignedAddress & 0x1FF_FFFF);
            case uint _ when alignedAddress is >= 0x0A00_0000 and <= 0x0BFF_FFFF:
                waitStates += _waitControl.WaitState1[seq] + _waitControl.WaitState1[1];
                return _gamePak.ReadWord(alignedAddress & 0x1FF_FFFF);
            case uint _ when alignedAddress is >= 0x0C00_0000 and <= 0x0DFF_FFFF:
                waitStates += _waitControl.WaitState2[seq] + _waitControl.WaitState2[1];
                return _gamePak.ReadWord(alignedAddress & 0x1FF_FFFF);
            case uint _ when alignedAddress is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                waitStates += _waitControl.SRAMWaitControl;
                return _gamePak.ReadSRam(unalignedAddress) * 0x01010101u;
            default:
                return D;
        };
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
                    case uint _ when address is >= 0x0400_0100 and <= 0x0400_0109:
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
                        return 0; // TODO - Is this correct?
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
        _debugger.Log("W {0:X8}={1:X8}", unalignedAddress, value);
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
                    case uint _ when alignedAddress is >= 0x0400_0100 and <= 0x0400_0108:
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
                        return 0; // TODO - Is this correct?
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
        _debugger.Log("W {0:X8}={1:X8}", unalignedAddress, value);
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
                    case uint _ when alignedAddress is >= 0x0400_0100 and <= 0x0400_0108:
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
                        return 0; // TODO - Is this correct?
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