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
public partial class MemoryBus
{
    private readonly Prefetcher _prefetcher;
    private readonly BaseDebugger _debugger;
    private readonly Ppu.Ppu _ppu;
    private readonly Apu.Apu _apu;
    private readonly Gamepad _gamepad;
    private readonly GamePak _gamePak;
    private readonly DmaDataUnit _dma;
    private readonly TimerController _timerController;
    private readonly InterruptRegisters _interruptRegisters;
    private readonly SerialController _serialController;
    private readonly Bios _bios;
    private readonly byte[] _onBoardWRam = new byte[0x4_0000];
    private readonly byte[] _onChipWRam = new byte[0x8000];
    private readonly WaitControl _waitControl;
    private InternalMemoryControl _intMemoryControl;
    public HaltMode HaltMode = HaltMode.None;
    public byte PostFlag = 0;

    internal MemoryBus(
        byte[] bios,
        Gamepad gamepad,
        GamePak gamePak,
        Ppu.Ppu ppu,
        Apu.Apu apu,
        DmaDataUnit dma,
        TimerController timerController,
        InterruptRegisters interruptRegisters,
        SerialController serialController,
        BaseDebugger debugger,
        bool skipBios)
    {
        _bios = new Bios(bios, skipBios);
        _ppu = ppu ?? throw new ArgumentNullException(nameof(ppu));
        _apu = apu ?? throw new ArgumentNullException(nameof(apu));
        _gamepad = gamepad ?? throw new ArgumentNullException(nameof(gamepad));
        _gamePak = gamePak ?? throw new ArgumentNullException(nameof(gamePak));
        _dma = dma ?? throw new ArgumentNullException(nameof(dma));
        _timerController = timerController ?? throw new ArgumentNullException(nameof(timerController));
        _interruptRegisters = interruptRegisters ?? throw new ArgumentNullException(nameof(interruptRegisters));
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _serialController = serialController ?? throw new ArgumentNullException(nameof(serialController));
        _waitControl = new WaitControl();
        _intMemoryControl = new InternalMemoryControl();
        _prefetcher = new Prefetcher(_waitControl, gamePak);

        if (skipBios)
        {
            PostFlag = 1;
        }

        // This is nasty. The best way to tell what size EEProm a cart has is
        // to use the DMA word count when it first read/writes to the cart. So
        // the cart needs a reference back to the dma unit which doesn't exist
        // when it's created. Evil code.
        _gamePak.SetDmaDataUnit(dma);
    }

    internal void Reset(bool skipBios)
    {
        Array.Clear(_onBoardWRam);
        Array.Clear(_onChipWRam);
        _waitControl.Reset();
        _intMemoryControl.Reset();
        _bios.Reset(skipBios);
        _prefetcher.Reset();
        HaltMode = HaltMode.None;
    }

    internal byte ReadByte(uint address, int seq, uint r15, uint D, ulong currentCycles, ref int waitStates)
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
                    uint _ when address is >= 0x0400_0060 and <= 0x0400_00A8 => _apu.ReadByte(address, D),
                    uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE => _dma.ReadByte(address, D),
                    uint _ when address is >= 0x0400_0100 and <= 0x0400_010F => _timerController.ReadByte(address),
                    uint _ when address is >= 0x0400_0120 and <= 0x0400_012C => _serialController.ReadByte(address),
                    uint _ when address is >= 0x0400_0130 and <= 0x0400_0132 => _gamepad.ReadByte(address),
                    uint _ when address is >= 0x0400_0134 and <= 0x0400_015A => _serialController.ReadByte(address),
                    IE => _interruptRegisters.ReadByte(address),
                    IF => _interruptRegisters.ReadByte(address),
                    WAITCNT => (byte)_waitControl.Get(),
                    IME => _interruptRegisters.ReadByte(address),
                    POSTFLG => PostFlag,
                    uint _ when (address & 0xFF00FFFF) == INTMEMCTRL => (byte)_intMemoryControl.Get(), // TODO - Do we just cast to byte here?
                    _ => (byte)D, // Open bus
                };
            case uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF:
                return _ppu.ReadByte(address);
            case uint _ when address is >= 0x0800_0000 and <= 0x09FF_FFFF:
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((address & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                return _prefetcher.ReadGamePakByte(address, _waitControl.WaitState0[seq], currentCycles, ref waitStates);
            case uint _ when address is >= 0x0A00_0000 and <= 0x0BFF_FFFF:
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((address & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                return _prefetcher.ReadGamePakByte(address, _waitControl.WaitState1[seq], currentCycles, ref waitStates);
            case uint _ when address is >= 0x0C00_0000 and <= 0x0DFF_FFFF:
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((address & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                return _prefetcher.ReadGamePakByte(address, _waitControl.WaitState2[seq], currentCycles, ref waitStates);
            case uint _ when address is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                _prefetcher.Reset();
                waitStates += _waitControl.SRAMWaitStates;
                return _gamePak.ReadBackupStorage(address);
            default:
                var rotate = (address & 0b11) * 8;
                return (byte)(D >> (int)rotate);
        };
    }

    internal ushort ReadHalfWord(uint unalignedAddress, int seq, uint r15, uint D, ulong currentCycles, ref int waitStates)
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
                    uint _ when alignedAddress is >= 0x0400_0060 and <= 0x0400_00A8 => _apu.ReadHalfWord(alignedAddress, D),
                    uint _ when alignedAddress is >= 0x0400_00B0 and <= 0x0400_00DE => _dma.ReadHalfWord(alignedAddress, D),
                    uint _ when alignedAddress is >= 0x0400_0100 and <= 0x0400_010E => _timerController.ReadHalfWord(alignedAddress),
                    uint _ when alignedAddress is >= 0x0400_0120 and <= 0x0400_012C => _serialController.ReadHalfWord(alignedAddress),
                    uint _ when alignedAddress is >= 0x0400_0130 and <= 0x0400_0132 => _gamepad.ReadHalfWord(alignedAddress),
                    uint _ when alignedAddress is >= 0x0400_0134 and <= 0x0400_015A => _serialController.ReadHalfWord(alignedAddress),
                    IE => _interruptRegisters.ReadHalfWord(alignedAddress),
                    IF => _interruptRegisters.ReadHalfWord(alignedAddress),
                    WAITCNT => _waitControl.Get(),
                    IME => _interruptRegisters.ReadHalfWord(alignedAddress),
                    POSTFLG => PostFlag,
                    uint _ when (alignedAddress & 0xFF00FFFF) == INTMEMCTRL => (ushort)_intMemoryControl.Get(), // TODO - Do we just cast to ushort here?
                    _ => (ushort)D, // Open bus
                };
            case uint _ when alignedAddress is >= 0x0500_0000 and <= 0x07FF_FFFF:
                return _ppu.ReadHalfWord(alignedAddress);
            case uint _ when alignedAddress is >= 0x0800_0000 and <= 0x09FF_FFFF:
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((alignedAddress & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                return _prefetcher.ReadGamePakHalfWord(alignedAddress, _waitControl.WaitState0[seq], currentCycles, ref waitStates);
            case uint _ when alignedAddress is >= 0x0A00_0000 and <= 0x0BFF_FFFF:
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((alignedAddress & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                return _prefetcher.ReadGamePakHalfWord(alignedAddress, _waitControl.WaitState1[seq], currentCycles, ref waitStates);
            case uint _ when alignedAddress is >= 0x0C00_0000 and <= 0x0DFF_FFFF:
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((alignedAddress & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                return _prefetcher.ReadGamePakHalfWord(alignedAddress, _waitControl.WaitState2[seq], currentCycles, ref waitStates);
            case uint _ when alignedAddress is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                _prefetcher.Reset();
                waitStates += _waitControl.SRAMWaitStates;
                return (ushort)(_gamePak.ReadBackupStorage(unalignedAddress) * 0x0101);
            default:
                // Open bus
                if ((unalignedAddress & 0b11) > 1)
                {
                    return (ushort)((D >> 16));
                }

                return (ushort)D;
        };
    }

    internal uint ReadWord(uint unalignedAddress, int seq, uint r15, uint D, ulong currentCycles, ref int waitStates)
    {
        var alignedAddress = unalignedAddress & 0xFFFF_FFFC;

        switch (alignedAddress)
        {
            case uint _ when alignedAddress <= 0x0000_3FFF:
                return _bios.ReadWord(unalignedAddress, r15);
            case uint _ when alignedAddress is >= 0x0200_0000 and <= 0x02FF_FFFF:
                waitStates += 1 + (_intMemoryControl.WaitControlWRAM * 2); // Additional wait state as 16 bit bus
                return Utils.ReadWord(_onBoardWRam, alignedAddress, 0x3_FFFF);
            case uint _ when alignedAddress is >= 0x0300_0000 and <= 0x03FF_FFFF:
                return Utils.ReadWord(_onChipWRam, alignedAddress, 0x7FFF);
            case uint _ when alignedAddress is >= 0x0400_0000 and <= 0x0400_03FE:
                return alignedAddress switch
                {
                    uint _ when alignedAddress is >= 0x0400_0000 and <= 0x0400_0056 => (uint)(_ppu.ReadRegisterHalfWord(alignedAddress, D) | (_ppu.ReadRegisterHalfWord(alignedAddress + 2, D) << 16)),
                    uint _ when alignedAddress is >= 0x0400_0060 and <= 0x0400_00A8 => _apu.ReadWord(alignedAddress, D),
                    uint _ when alignedAddress is >= 0x0400_00B0 and <= 0x0400_00DE => _dma.ReadWord(alignedAddress, D),
                    uint _ when alignedAddress is >= 0x0400_0100 and <= 0x0400_010E => _timerController.ReadWord(alignedAddress),
                    uint _ when alignedAddress is >= 0x0400_0120 and <= 0x0400_012C => _serialController.ReadWord(alignedAddress),
                    uint _ when alignedAddress is >= 0x0400_0130 and <= 0x0400_0132 => (uint)(_gamepad.ReadHalfWord(alignedAddress) | (_gamepad.ReadHalfWord(alignedAddress + 2) << 16)),
                    uint _ when alignedAddress is >= 0x0400_0134 and <= 0x0400_015A => _serialController.ReadWord(alignedAddress),
                    IE => _interruptRegisters.ReadWord(alignedAddress),
                    WAITCNT => _waitControl.Get(),
                    IME => _interruptRegisters.ReadWord(alignedAddress),
                    POSTFLG => PostFlag,
                    uint _ when (alignedAddress & 0xFF00FFFF) == INTMEMCTRL => _intMemoryControl.Get(),
                    _ => D, // Open bus
                };
            case uint a when a is >= 0x0500_0000 and <= 0x06FF_FFFF:
                waitStates += 1;
                return (uint)(_ppu.ReadHalfWord(alignedAddress) | (_ppu.ReadHalfWord(alignedAddress + 2) << 16));
            case uint a when a is >= 0x0700_0000 and <= 0x07FF_FFFF:
                return (uint)(_ppu.ReadHalfWord(alignedAddress) | (_ppu.ReadHalfWord(alignedAddress + 2) << 16));
            case uint _ when alignedAddress is >= 0x0800_0000 and <= 0x09FF_FFFF:
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((alignedAddress & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                return _prefetcher.ReadGamePakWord(alignedAddress, _waitControl.WaitState0[seq], _waitControl.WaitState0[1] + 1, currentCycles, ref waitStates);
            case uint _ when alignedAddress is >= 0x0A00_0000 and <= 0x0BFF_FFFF:
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((alignedAddress & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                return _prefetcher.ReadGamePakWord(alignedAddress, _waitControl.WaitState1[seq], _waitControl.WaitState1[1] + 1, currentCycles, ref waitStates);
            case uint _ when alignedAddress is >= 0x0C00_0000 and <= 0x0DFF_FFFF:
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((alignedAddress & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                return _prefetcher.ReadGamePakWord(alignedAddress, _waitControl.WaitState2[seq], _waitControl.WaitState2[1] + 1, currentCycles, ref waitStates);
            case uint _ when alignedAddress is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                _prefetcher.Reset();
                waitStates += _waitControl.SRAMWaitStates;
                return _gamePak.ReadBackupStorage(unalignedAddress) * 0x01010101u;
            default:
                return D;
        };
    }

    internal int WriteByte(uint address, byte value, int seq, uint r15)
    {
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
                        _apu.WriteByte(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE:
                        _dma.WriteByte(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0100 and <= 0x0400_010F:
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
                        _waitControl.SetByte1(value);
                        return 0;
                    case WAITCNT + 1:
                        _waitControl.SetByte2(value);
                        return 0;
                    case IME:
                    case IE:
                    case IF:
                        _interruptRegisters.WriteByte(address, value);
                        return 0;
                    case POSTFLG:
                        if (r15 < 0x3FFF)
                        {
                            PostFlag = 1;
                        }
                        return 0;
                    case HALTCNT:
                        if (r15 <= 0x3FFF)
                        {
                            HaltMode = (HaltMode)((value >> 7) & 0b1);
                        }
                        return 0;
                    case uint _ when (address & 0xFF00FFFF) == INTMEMCTRL:
                        throw new NotImplementedException("Can't set int memory control as byte or wait control for WRAM will get locked up at 15");
                    default:
                        return 0;
                };
            case uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF:
                _ppu.WriteByte(address, value);
                return 0;
            case uint _ when address is >= 0x0800_0000 and <= 0x09FF_FFFF:
                _prefetcher.Reset();
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((address & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                _gamePak.Write(address, value);
                return _waitControl.WaitState0[seq];
            case uint _ when address is >= 0x0A00_0000 and <= 0x0BFF_FFFF:
                _prefetcher.Reset();
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((address & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                _gamePak.Write(address, value);
                return _waitControl.WaitState1[seq];
            case uint _ when address is >= 0x0C00_0000 and <= 0x0DFF_FFFF:
                _prefetcher.Reset();
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((address & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                _gamePak.Write(address, value);
                return _waitControl.WaitState2[seq];
            case uint _ when address is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                _prefetcher.Reset();
                _gamePak.WriteBackupStorage(address, value);
                return _waitControl.SRAMWaitStates;
            default:
                return 0;
        }
    }

    internal int WriteHalfWord(uint unalignedAddress, ushort value, int seq, uint r15)
    {
        var alignedAddress = unalignedAddress & 0xFFFF_FFFE;
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
                        _apu.WriteHalfWord(alignedAddress, value);
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_00B0 and <= 0x0400_00DE:
                        _dma.WriteHalfWord(alignedAddress, value);
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_0100 and <= 0x0400_010E:
                        _timerController.WriteHalfWord(alignedAddress, value);
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
                        if (r15 <= 0x3FFF)
                        {
                            PostFlag = 1;
                            HaltMode = (HaltMode)((value >> 15) & 0b1);
                        }
                        return 0;
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
                        return 0;
                };
            case uint _ when alignedAddress is >= 0x0500_0000 and <= 0x07FF_FFFF:
                _ppu.WriteHalfWord(alignedAddress, value);
                return 0;
            case uint _ when alignedAddress is >= 0x0800_0000 and <= 0x09FF_FFFF:
                _prefetcher.Reset();
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((alignedAddress & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                _gamePak.Write(alignedAddress, (byte)value);
                return _waitControl.WaitState0[seq];
            case uint _ when alignedAddress is >= 0x0A00_0000 and <= 0x0BFF_FFFF:
                _prefetcher.Reset();
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((alignedAddress & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                _gamePak.Write(alignedAddress, (byte)value);
                return _waitControl.WaitState1[seq];
            case uint _ when alignedAddress is >= 0x0C00_0000 and <= 0x0DFF_FFFF:
                _prefetcher.Reset();
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((alignedAddress & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                _gamePak.Write(alignedAddress, (byte)value);
                return _waitControl.WaitState2[seq];
            case uint _ when alignedAddress is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                _prefetcher.Reset();
                var shift = (ushort)(unalignedAddress & 1) << 3;
                _gamePak.WriteBackupStorage(unalignedAddress, (byte)(value >> shift));
                return _waitControl.SRAMWaitStates;
            default:
                return 0;
        }
    }

    internal int WriteWord(uint unalignedAddress, uint value, int seq, uint r15)
    {
        var alignedAddress = unalignedAddress & 0xFFFF_FFFC;

        switch (alignedAddress)
        {
            case uint _ when alignedAddress <= 0x0000_3FFF:
                return 0;
            case uint _ when alignedAddress is >= 0x0200_0000 and <= 0x02FF_FFFF:
                Utils.WriteWord(_onBoardWRam, 0x3_FFFF, alignedAddress, value);
                return 1 + (_intMemoryControl.WaitControlWRAM * 2); // Extra cycle as this is a 16 bit bus
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
                        _apu.WriteWord(alignedAddress, value);
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_00B0 and <= 0x0400_00DE:
                        _dma.WriteWord(alignedAddress, value);
                        return 0;
                    case uint _ when alignedAddress is >= 0x0400_0100 and <= 0x0400_010E:
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
                        if (r15 <= 0x3FFF)
                        {
                            PostFlag = 1;
                            HaltMode = (HaltMode)((value >> 15) & 0b1);
                        }
                        return 0;
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
                };
            case uint _ when alignedAddress is >= 0x0500_0000 and <= 0x06FF_FFFF:
                _ppu.WriteHalfWord(alignedAddress, (ushort)value);
                _ppu.WriteHalfWord(alignedAddress + 2, (ushort)(value >> 16));
                return 1;
            case uint _ when alignedAddress is >= 0x0700_0000 and <= 0x07FF_FFFF:
                _ppu.WriteHalfWord(alignedAddress, (ushort)value);
                _ppu.WriteHalfWord(alignedAddress + 2, (ushort)(value >> 16));
                return 0;
            case uint _ when alignedAddress is >= 0x0800_0000 and <= 0x09FF_FFFF:
                _prefetcher.Reset();
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((alignedAddress & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                _gamePak.Write(alignedAddress, (byte)value);
                return _waitControl.WaitState0[seq] + _waitControl.WaitState0[1] + 1;
            case uint _ when alignedAddress is >= 0x0A00_0000 and <= 0x0BFF_FFFF:
                _prefetcher.Reset();
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((alignedAddress & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                _gamePak.Write(alignedAddress, (byte)value);
                return _waitControl.WaitState1[seq] + _waitControl.WaitState1[1] + 1;
            case uint _ when alignedAddress is >= 0x0C00_0000 and <= 0x0DFF_FFFF:
                _prefetcher.Reset();
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                if ((alignedAddress & 0x1_FFFF) == 0)
                {
                    seq = 0;
                }
                _gamePak.Write(alignedAddress, (byte)value);
                return _waitControl.WaitState2[seq] + _waitControl.WaitState2[1] + 1;
            case uint _ when alignedAddress is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                _prefetcher.Reset();
                var shift = (int)(unalignedAddress & 0b11) << 3;
                _gamePak.WriteBackupStorage(unalignedAddress, (byte)(value >> shift));
                return _waitControl.SRAMWaitStates * 2;
            default:
                return 0;
        }
    }
}