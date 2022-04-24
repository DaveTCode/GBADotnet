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
    public readonly Prefetcher _prefetcher;
    public readonly BaseDebugger _debugger;
    public readonly Ppu.Ppu _ppu;
    public readonly Apu.Apu _apu;
    public readonly Gamepad _gamepad;
    public readonly GamePak _gamePak;
    public readonly DmaDataUnit _dma;
    public readonly TimerController _timerController;
    public readonly InterruptRegisters _interruptRegisters;
    public readonly SerialController _serialController;
    public readonly Bios _bios;
    public readonly byte[] OnBoardWRam = new byte[0x4_0000];
    public readonly byte[] OnChipWRam = new byte[0x8000];
    public readonly WaitControl _waitControl;
    public InternalMemoryControl _intMemoryControl;
    public HaltMode HaltMode = HaltMode.None;
    public byte PostFlag = 0;
    public int WaitStates = 0;
    public bool InUseByDma;

    public MemoryBus(
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
        Array.Clear(OnBoardWRam);
        Array.Clear(OnChipWRam);
        _waitControl.Reset();
        _intMemoryControl.Reset();
        _bios.Reset(skipBios);
        _prefetcher.Reset();
        HaltMode = HaltMode.None;

        PostFlag = skipBios ? (byte)1 : (byte)0;
        WaitStates = 0;
        InUseByDma = false;
    }

    internal byte ReadByte(uint address, int seq, uint r15, uint D, long currentCycles, bool isCodeRead)
    {
        switch (address)
        {
            case uint _ when address <= 0x0000_3FFF:
                return _bios.ReadByte(address, r15);
            case uint _ when address is >= 0x0200_0000 and <= 0x02FF_FFFF:
                WaitStates += _intMemoryControl.WaitControlWRAM;
                return OnBoardWRam[address & 0x3_FFFF];
            case uint _ when address is >= 0x0300_0000 and <= 0x03FF_FFFF:
                return OnChipWRam[address & 0x7FFF];
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
                return _prefetcher.ReadGamePakByte(address, 0, seq, currentCycles, ref WaitStates, isCodeRead);
            case uint _ when address is >= 0x0A00_0000 and <= 0x0BFF_FFFF:
                return _prefetcher.ReadGamePakByte(address, 1, seq, currentCycles, ref WaitStates, isCodeRead);
            case uint _ when address is >= 0x0C00_0000 and <= 0x0DFF_FFFF:
                return _prefetcher.ReadGamePakByte(address, 2, seq, currentCycles, ref WaitStates, isCodeRead);
            case uint _ when address is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                _prefetcher.Reset();
                WaitStates += _waitControl.SRAMWaitStates;
                return _gamePak.ReadBackupStorage(address);
            default:
                var rotate = (address & 0b11) * 8;
                return (byte)(D >> (int)rotate);
        };
    }

    internal ushort ReadHalfWord(uint unalignedAddress, int seq, uint r15, uint D, long currentCycles, bool isCodeRead)
    {
        var alignedAddress = unalignedAddress & 0xFFFF_FFFE;

        switch (alignedAddress)
        {
            case uint _ when alignedAddress <= 0x0000_3FFF:
                return _bios.ReadHalfWord(unalignedAddress, r15);
            case uint _ when alignedAddress is >= 0x0200_0000 and <= 0x02FF_FFFF:
                WaitStates += _intMemoryControl.WaitControlWRAM;
                return Utils.ReadHalfWord(OnBoardWRam, alignedAddress, 0x3_FFFF);
            case uint a when a is >= 0x0300_0000 and <= 0x03FF_FFFF:
                return Utils.ReadHalfWord(OnChipWRam, alignedAddress, 0x7FFF);
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
                return _prefetcher.ReadGamePakHalfWord(alignedAddress, 0, seq, currentCycles, ref WaitStates, isCodeRead);
            case uint _ when alignedAddress is >= 0x0A00_0000 and <= 0x0BFF_FFFF:
                return _prefetcher.ReadGamePakHalfWord(alignedAddress, 1, seq, currentCycles, ref WaitStates, isCodeRead);
            case uint _ when alignedAddress is >= 0x0C00_0000 and <= 0x0DFF_FFFF:
                return _prefetcher.ReadGamePakHalfWord(alignedAddress, 2, seq, currentCycles, ref WaitStates, isCodeRead);
            case uint _ when alignedAddress is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                _prefetcher.Reset();
                WaitStates += _waitControl.SRAMWaitStates;
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

    internal uint ReadWord(uint unalignedAddress, int seq, uint r15, uint D, long currentCycles, bool isCodeRead)
    {
        var alignedAddress = unalignedAddress & 0xFFFF_FFFC;

        switch (alignedAddress)
        {
            case uint _ when alignedAddress <= 0x0000_3FFF:
                return _bios.ReadWord(unalignedAddress, r15);
            case uint _ when alignedAddress is >= 0x0200_0000 and <= 0x02FF_FFFF:
                WaitStates += 1 + (_intMemoryControl.WaitControlWRAM * 2); // Additional wait state as 16 bit bus
                return Utils.ReadWord(OnBoardWRam, alignedAddress, 0x3_FFFF);
            case uint _ when alignedAddress is >= 0x0300_0000 and <= 0x03FF_FFFF:
                return Utils.ReadWord(OnChipWRam, alignedAddress, 0x7FFF);
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
                WaitStates += 1;
                return (uint)(_ppu.ReadHalfWord(alignedAddress) | (_ppu.ReadHalfWord(alignedAddress + 2) << 16));
            case uint a when a is >= 0x0700_0000 and <= 0x07FF_FFFF:
                return (uint)(_ppu.ReadHalfWord(alignedAddress) | (_ppu.ReadHalfWord(alignedAddress + 2) << 16));
            case uint _ when alignedAddress is >= 0x0800_0000 and <= 0x09FF_FFFF:
                return _prefetcher.ReadGamePakWord(alignedAddress, 0, seq, currentCycles, ref WaitStates, isCodeRead);
            case uint _ when alignedAddress is >= 0x0A00_0000 and <= 0x0BFF_FFFF:
                return _prefetcher.ReadGamePakWord(alignedAddress, 1, seq, currentCycles, ref WaitStates, isCodeRead);
            case uint _ when alignedAddress is >= 0x0C00_0000 and <= 0x0DFF_FFFF:
                return _prefetcher.ReadGamePakWord(alignedAddress, 2, seq, currentCycles, ref WaitStates, isCodeRead);
            case uint _ when alignedAddress is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                _prefetcher.Reset();
                WaitStates += _waitControl.SRAMWaitStates;
                return _gamePak.ReadBackupStorage(unalignedAddress) * 0x01010101u;
            default:
                return D;
        };
    }

    internal void WriteByte(uint address, byte value, int seq, uint r15)
    {
        switch (address)
        {
            case uint _ when address <= 0x0000_3FFF:
                break;
            case uint _ when address is >= 0x0200_0000 and <= 0x02FF_FFFF:
                OnBoardWRam[address & 0x3_FFFF] = value;
                WaitStates += _intMemoryControl.WaitControlWRAM;
                break;
            case uint _ when address is >= 0x0300_0000 and <= 0x03FF_FFFF:
                OnChipWRam[address & 0x7FFF] = value;
                break;
            case uint _ when address is >= 0x0400_0000 and <= 0x0400_03FE:
                switch (address)
                {
                    case uint _ when address is >= 0x0400_0000 and <= 0x0400_0056:
                        _ppu.WriteRegisterByte(address, value);
                        break;
                    case uint _ when address is >= 0x0400_0060 and <= 0x0400_00A8:
                        _apu.WriteByte(address, value);
                        break;
                    case uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE:
                        _dma.WriteByte(address, value);
                        break;
                    case uint _ when address is >= 0x0400_0100 and <= 0x0400_010F:
                        _timerController.WriteByte(address, value);
                        break;
                    case uint _ when address is >= 0x0400_0120 and <= 0x0400_012C:
                        _serialController.WriteByte(address, value);
                        break;
                    case uint _ when address is >= 0x0400_0130 and <= 0x0400_0132:
                        _gamepad.WriteByte(address, value);
                        break;
                    case uint _ when address is >= 0x0400_0134 and <= 0x0400_015A:
                        _serialController.WriteByte(address, value);
                        break;
                    case WAITCNT:
                        _waitControl.SetByte1(value);
                        break;
                    case WAITCNT + 1:
                        _waitControl.SetByte2(value);
                        break;
                    case IME:
                    case IE:
                    case IF:
                        _interruptRegisters.WriteByte(address, value);
                        break;
                    case POSTFLG:
                        if (r15 < 0x3FFF)
                        {
                            PostFlag = 1;
                        }
                        break;
                    case HALTCNT:
                        if (r15 <= 0x3FFF)
                        {
                            HaltMode = (HaltMode)((value >> 7) & 0b1);
                        }
                        break;
                    case uint _ when (address & 0xFF00FFFF) == INTMEMCTRL:
                        throw new NotImplementedException("Can't set int memory control as byte or wait control for WRAM will get locked up at 15");
                    default:
                        break;
                };
                break;
            case uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF:
                _ppu.WriteByte(address, value);
                break;
            case uint _ when address is >= 0x0800_0000 and <= 0x09FF_FFFF:
                _prefetcher.Write(address, value, seq, 2, 0, ref WaitStates);
                break;
            case uint _ when address is >= 0x0A00_0000 and <= 0x0BFF_FFFF:
                _prefetcher.Write(address, value, seq, 2, 1, ref WaitStates);
                break;
            case uint _ when address is >= 0x0C00_0000 and <= 0x0DFF_FFFF:
                _prefetcher.Write(address, value, seq, 2, 2, ref WaitStates);
                break;
            case uint _ when address is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                _prefetcher.Reset();
                _gamePak.WriteBackupStorage(address, value);
                WaitStates += _waitControl.SRAMWaitStates;
                break;
            default:
                break;
        }
    }

    internal void WriteHalfWord(uint unalignedAddress, ushort value, int seq, uint r15)
    {
        var alignedAddress = unalignedAddress & 0xFFFF_FFFE;
        switch (alignedAddress)
        {
            case uint _ when alignedAddress <= 0x0000_3FFF:
                break;
            case uint _ when alignedAddress is >= 0x0200_0000 and <= 0x02FF_FFFF:
                Utils.WriteHalfWord(OnBoardWRam, 0x3_FFFF, alignedAddress, value);
                WaitStates += _intMemoryControl.WaitControlWRAM;
                break;
            case uint _ when alignedAddress is >= 0x0300_0000 and <= 0x03FF_FFFF:
                Utils.WriteHalfWord(OnChipWRam, 0x7FFF, alignedAddress, value);
                break;
            case uint _ when alignedAddress is >= 0x0400_0000 and <= 0x0400_03FE:
                switch (alignedAddress)
                {
                    case uint _ when alignedAddress is >= 0x0400_0000 and <= 0x0400_0056:
                        _ppu.WriteRegisterHalfWord(alignedAddress, value);
                        break;
                    case uint _ when alignedAddress is >= 0x0400_0060 and <= 0x0400_00A8:
                        _apu.WriteHalfWord(alignedAddress, value);
                        break;
                    case uint _ when alignedAddress is >= 0x0400_00B0 and <= 0x0400_00DE:
                        _dma.WriteHalfWord(alignedAddress, value);
                        break;
                    case uint _ when alignedAddress is >= 0x0400_0100 and <= 0x0400_010E:
                        _timerController.WriteHalfWord(alignedAddress, value);
                        break;
                    case uint _ when alignedAddress is >= 0x0400_0120 and <= 0x0400_012C:
                        _serialController.WriteHalfWord(alignedAddress, value);
                        break;
                    case uint _ when alignedAddress is >= 0x0400_0130 and <= 0x0400_0132:
                        _gamepad.WriteHalfWord(alignedAddress, value);
                        break;
                    case uint _ when alignedAddress is >= 0x0400_0134 and <= 0x0400_015A:
                        _serialController.WriteHalfWord(alignedAddress, value);
                        break;
                    case POSTFLG:
                        if (r15 <= 0x3FFF)
                        {
                            PostFlag = 1;
                            HaltMode = (HaltMode)((value >> 15) & 0b1);
                        }
                        break;
                    case WAITCNT:
                        _waitControl.Set(value);
                        break;
                    case IME:
                    case IE:
                    case IF:
                        _interruptRegisters.WriteHalfWord(alignedAddress, value);
                        break;
                    case uint _ when (alignedAddress & 0xFF00FFFF) == INTMEMCTRL:
                        throw new NotImplementedException("Can't set int memory control as ushort or wait control for WRAM will get locked up at 15");
                    default:
                        break;
                };
                break;
            case uint _ when alignedAddress is >= 0x0500_0000 and <= 0x07FF_FFFF:
                _ppu.WriteHalfWord(alignedAddress, value);
                break;
            case uint _ when alignedAddress is >= 0x0800_0000 and <= 0x09FF_FFFF:
                _prefetcher.Write(alignedAddress, (byte)value, seq, 2, 0, ref WaitStates);
                break;
            case uint _ when alignedAddress is >= 0x0A00_0000 and <= 0x0BFF_FFFF:
                _prefetcher.Write(alignedAddress, (byte)value, seq, 2, 1, ref WaitStates);
                break;
            case uint _ when alignedAddress is >= 0x0C00_0000 and <= 0x0DFF_FFFF:
                _prefetcher.Write(alignedAddress, (byte)value, seq, 2, 2, ref WaitStates);
                break;
            case uint _ when alignedAddress is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                _prefetcher.Reset();
                var shift = (ushort)(unalignedAddress & 1) << 3;
                _gamePak.WriteBackupStorage(unalignedAddress, (byte)(value >> shift));
                WaitStates += _waitControl.SRAMWaitStates;
                break;
            default:
                break;
        }
    }

    internal void WriteWord(uint unalignedAddress, uint value, int seq, uint r15)
    {
        var alignedAddress = unalignedAddress & 0xFFFF_FFFC;

        switch (alignedAddress)
        {
            case uint _ when alignedAddress <= 0x0000_3FFF:
                break;
            case uint _ when alignedAddress is >= 0x0200_0000 and <= 0x02FF_FFFF:
                Utils.WriteWord(OnBoardWRam, 0x3_FFFF, alignedAddress, value);
                WaitStates += 1 + (_intMemoryControl.WaitControlWRAM * 2); // Extra cycle as this is a 16 bit bus
                break;
            case uint _ when alignedAddress is >= 0x0300_0000 and <= 0x03FF_FFFF:
                Utils.WriteWord(OnChipWRam, 0x7FFF, alignedAddress, value);
                break;
            case uint _ when alignedAddress is >= 0x0400_0000 and <= 0x0400_03FE:
                switch (alignedAddress)
                {
                    case uint _ when alignedAddress is >= 0x0400_0000 and <= 0x0400_0056:
                        _ppu.WriteRegisterHalfWord(alignedAddress, (ushort)value);
                        _ppu.WriteRegisterHalfWord(alignedAddress + 2, (ushort)(value >> 16));
                        break;
                    case uint _ when alignedAddress is >= 0x0400_0060 and <= 0x0400_00A8:
                        _apu.WriteWord(alignedAddress, value);
                        break;
                    case uint _ when alignedAddress is >= 0x0400_00B0 and <= 0x0400_00DE:
                        _dma.WriteWord(alignedAddress, value);
                        break;
                    case uint _ when alignedAddress is >= 0x0400_0100 and <= 0x0400_010E:
                        _timerController.WriteWord(alignedAddress, value);
                        break;
                    case uint _ when alignedAddress is >= 0x0400_0120 and <= 0x0400_012C:
                        _serialController.WriteWord(alignedAddress, value);
                        break;
                    case uint _ when alignedAddress is >= 0x0400_0130 and <= 0x0400_0132:
                        _gamepad.WriteHalfWord(alignedAddress, (ushort)value);
                        _gamepad.WriteHalfWord(alignedAddress + 2, (ushort)(value >> 16));
                        break;
                    case uint _ when alignedAddress is >= 0x0400_0134 and <= 0x0400_015A:
                        _serialController.WriteWord(alignedAddress, value);
                        break;
                    case POSTFLG:
                        if (r15 <= 0x3FFF)
                        {
                            PostFlag = 1;
                            HaltMode = (HaltMode)((value >> 15) & 0b1);
                        }
                        break;
                    case WAITCNT:
                        _waitControl.Set((ushort)value);
                        // 206 is unused so no extra write here
                        break;
                    case IME:
                    case IE:
                    case IF:
                        _interruptRegisters.WriteWord(alignedAddress, value);
                        break;
                    case uint _ when (alignedAddress & 0xFF00FFFF) == INTMEMCTRL:
                        _intMemoryControl.Set(value);
                        break;
                    default:
                        break;
                };
                break;
            case uint _ when alignedAddress is >= 0x0500_0000 and <= 0x06FF_FFFF:
                _ppu.WriteHalfWord(alignedAddress, (ushort)value);
                _ppu.WriteHalfWord(alignedAddress + 2, (ushort)(value >> 16));
                WaitStates += 1;
                break;
            case uint _ when alignedAddress is >= 0x0700_0000 and <= 0x07FF_FFFF:
                _ppu.WriteHalfWord(alignedAddress, (ushort)value);
                _ppu.WriteHalfWord(alignedAddress + 2, (ushort)(value >> 16));
                break;
            case uint _ when alignedAddress is >= 0x0800_0000 and <= 0x09FF_FFFF:
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                _prefetcher.Write(alignedAddress, (byte)value, seq, 4, 0, ref WaitStates);
                break;
            case uint _ when alignedAddress is >= 0x0A00_0000 and <= 0x0BFF_FFFF:
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                _prefetcher.Write(alignedAddress, (byte)value, seq, 4, 1, ref WaitStates);
                break;
            case uint _ when alignedAddress is >= 0x0C00_0000 and <= 0x0DFF_FFFF:
                // "The GBA forcefully uses non-sequential timing at the beginning of each 128K-block of gamepak ROM"
                _prefetcher.Write(alignedAddress, (byte)value, seq, 4, 2, ref WaitStates);
                break;
            case uint _ when alignedAddress is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                _prefetcher.Reset();
                var shift = (int)(unalignedAddress & 0b11) << 3;
                _gamePak.WriteBackupStorage(unalignedAddress, (byte)(value >> shift));
                WaitStates += _waitControl.SRAMWaitStates * 2;
                break;
            default:
                break;
        }
    }
}