using GameboyAdvanced.Core.Cpu.Interrupts;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Dma;
using GameboyAdvanced.Core.Input;
using GameboyAdvanced.Core.Rom;
using GameboyAdvanced.Core.Timer;

namespace GameboyAdvanced.Core;

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
    private readonly DmaController _dma;
    private readonly TimerController _timerController;
    private readonly InterruptWaitStateAndPowerControlRegisters _interruptController;
    private readonly byte[] _bios = new byte[0x4000];
    private readonly byte[] _onBoardWRam = new byte[0x4_0000];
    private readonly byte[] _onChipWRam = new byte[0x8000];

    internal void Reset()
    {
        Array.Clear(_onBoardWRam);
        Array.Clear(_onChipWRam);
    }

    internal MemoryBus(byte[] bios, Gamepad gamepad, GamePak gamePak, Ppu.Ppu ppu, DmaController dma, TimerController timerController, InterruptWaitStateAndPowerControlRegisters interruptController, BaseDebugger debugger)
    {
        if (bios == null || bios.Length > _bios.Length) throw new ArgumentException($"Bios is invalid length {bios?.Length}", nameof(bios));
        Array.Fill<byte>(_bios, 0);
        Array.Copy(bios, 0, _bios, 0, Math.Min(_bios.Length, bios.Length));

        _ppu = ppu ?? throw new ArgumentNullException(nameof(ppu));
        _gamepad = gamepad ?? throw new ArgumentNullException(nameof(gamepad));
        _gamePak = gamePak ?? throw new ArgumentNullException(nameof(gamePak));
        _dma = dma ?? throw new ArgumentNullException(nameof(dma));
        _timerController = timerController ?? throw new ArgumentNullException(nameof(timerController));
        _interruptController = interruptController ?? throw new ArgumentNullException(nameof(interruptController));
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
    }

    internal (byte, int) ReadByte(uint address)
    {
        var (val, waitStates) = address switch
        {
            uint _ when address <= 0x0000_3FFF => (_bios[address], 0), // TODO - Can only read from bios when IP is located in BIOS region
            uint _ when address is >= 0x0200_0000 and <= 0x0203_FFFF => (_onBoardWRam[address & 0x3_FFFF], 2),
            uint _ when address is >= 0x0300_0000 and <= 0x0300_7FFF => (_onChipWRam[address & 0x7FFF], 0),
            uint _ when address is >= 0x0400_0000 and <= 0x0400_03FE => address switch
            {
                uint _ when address is >= 0x0400_0000 and <= 0x0400_0056 => (_ppu.ReadRegisterByte(address), 0),
                uint _ when address is >= 0x0400_0060 and <= 0x0400_00A8 => throw new NotImplementedException("Sound registers not yet implemented"),
                uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE => (_dma.ReadByte(address), 0),
                uint _ when address is >= 0x0400_0100 and <= 0x0400_0110 => _timerController.ReadByte(address),
                uint _ when address is >= 0x0400_0120 and <= 0x0400_012C => throw new NotImplementedException("Serial comms registers not yet implemented"),
                uint _ when address is >= 0x0400_0130 and <= 0x0400_0132 => (_gamepad.ReadByte(address), 0),
                uint _ when address is >= 0x0400_0134 and <= 0x0400_015A => throw new NotImplementedException("Serial comms registers not yet implemented"),
                uint _ when address is >= 0x0400_0200 and <= 0x0470_0000 => _interruptController.ReadByte(address),
                _ => throw new ArgumentOutOfRangeException(nameof(address), $"IO registers at {address:X8} not mapped"),
            },
            uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF => (_ppu.ReadByte(address), 0),
            uint _ when address is >= 0x0800_0000 and <= 0x0FFF_FFFF => _gamePak.ReadByte(address),
            _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} not memory mapped")
        };

        #if DEBUG
        _debugger.Log($"R {address:X8}={val:X2}");
        #endif
        return (val, waitStates);
    }

    internal (ushort, int) ReadHalfWord(uint address)
    {
        var (val, waitStates) = address switch
        {
            uint a when a <= 0x0000_3FFF => (Utils.ReadHalfWord(_bios, address, 0x3FFF), 0), // TODO - Can only read from bios when IP is located in BIOS region
            uint a when a is >= 0x0200_0000 and <= 0x0203_FFFF => (Utils.ReadHalfWord(_onBoardWRam, address, 0x3_FFFF), 2),
            uint a when a is >= 0x0300_0000 and <= 0x0300_7FFF => (Utils.ReadHalfWord(_onChipWRam, address, 0x7FFF), 0),
            uint a when a is >= 0x0400_0000 and <= 0x0400_03FE => address switch
            {
                uint _ when address is >= 0x0400_0000 and <= 0x0400_0056 => (_ppu.ReadRegisterHalfWord(address), 0),
                uint _ when address is >= 0x0400_0060 and <= 0x0400_00A8 => throw new NotImplementedException("Sound registers not yet implemented"),
                uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE => (_dma.ReadHalfWord(address), 0),
                uint _ when address is >= 0x0400_0100 and <= 0x0400_0110 => _timerController.ReadHalfWord(address),
                uint _ when address is >= 0x0400_0120 and <= 0x0400_012C => throw new NotImplementedException("Serial comms registers not yet implemented"),
                uint _ when address is >= 0x0400_0130 and <= 0x0400_0132 => (_gamepad.ReadHalfWord(address), 0),
                uint _ when address is >= 0x0400_0134 and <= 0x0400_015A => throw new NotImplementedException("Serial comms registers not yet implemented"),
                uint _ when address is >= 0x0400_0200 and <= 0x0470_0000 => _interruptController.ReadHalfWord(address),
                _ => throw new ArgumentOutOfRangeException(nameof(address), $"IO registers at {address:X8} not mapped"),
            },
            uint a when a is >= 0x0500_0000 and <= 0x07FF_FFFF => (_ppu.ReadHalfWord(address), 0),
            uint a when a is >= 0x0800_0000 and <= 0x0FFF_FFFF => _gamePak.ReadHalfWord(address),
            _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} not memory mapped")
        };

#if DEBUG
        _debugger.Log($"R {address:X8}={val:X4}");
#endif
        return (val, waitStates);
    }

    internal (uint, int) ReadWord(uint address)
    {
        var (val, waitStates) = address switch
        {
            uint a when a <= 0x0000_3FFF => (Utils.ReadWord(_bios, address, 0x3FFF), 0), // TODO - Can only read from bios when IP is located in BIOS region
            uint a when a is >= 0x0200_0000 and <= 0x0203_FFFF => (Utils.ReadWord(_onBoardWRam, address, 0x3_FFFF), 5),
            uint a when a is >= 0x0300_0000 and <= 0x0300_7FFF => (Utils.ReadWord(_onChipWRam, address, 0x7FFF), 0),
            uint a when a is >= 0x0400_0000 and <= 0x0400_03FE => address switch
            {
                uint _ when address is >= 0x0400_0000 and <= 0x0400_0056 => ((uint)(_ppu.ReadRegisterHalfWord(address) | (_ppu.ReadRegisterHalfWord(address + 2) << 16)), 1), // TODO - not really a wait state?
                uint _ when address is >= 0x0400_0060 and <= 0x0400_00A8 => throw new NotImplementedException("Sound registers not yet implemented"),
                uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE => (_dma.ReadWord(address), 0),
                uint _ when address is >= 0x0400_0100 and <= 0x0400_0110 => _timerController.ReadWord(address),
                uint _ when address is >= 0x0400_0120 and <= 0x0400_012C => throw new NotImplementedException("Serial comms registers not yet implemented"),
                uint _ when address is >= 0x0400_0130 and <= 0x0400_0132 => ((uint)(_gamepad.ReadHalfWord(address) | (_gamepad.ReadHalfWord(address + 2) << 16)), 0),
                uint _ when address is >= 0x0400_0134 and <= 0x0400_015A => throw new NotImplementedException("Serial comms registers not yet implemented"),
                uint _ when address is >= 0x0400_0200 and <= 0x0470_0000 => _interruptController.ReadWord(address),
                _ => throw new ArgumentOutOfRangeException(nameof(address), $"IO registers at {address:X8} not mapped"),
            },
            uint a when a is >= 0x0500_0000 and <= 0x07FF_FFFF => ((uint)(_ppu.ReadHalfWord(address) | (_ppu.ReadHalfWord(address + 2) << 16)), 1),
            uint a when a is >= 0x0800_0000 and <= 0x0FFF_FFFF => _gamePak.ReadWord(address),
            _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} not memory mapped")
        };

#if DEBUG
        _debugger.Log($"R {address:X8}={val:X8}");
#endif
        return (val, waitStates);
    }

    internal int WriteByte(uint address, byte value)
    {
#if DEBUG
        _debugger.Log($"W {address:X8}={value:X2}");
#endif
        switch (address)
        {
            case uint _ when address <= 0x0000_3FFF:
                return 0;
            case uint _ when address is >= 0x0200_0000 and <= 0x0203_FFFF:
                _onBoardWRam[address & 0x3_FFFF] = value;
                return 2;
            case uint _ when address is >= 0x0300_0000 and <= 0x0300_7FFF:
                _onChipWRam[address & 0x7FFF] = value;
                return 0;
            case uint _ when address is >= 0x0400_0000 and <= 0x0400_03FE:
                switch (address)
                {
                    case uint _ when address is >= 0x0400_0000 and <= 0x0400_0056:
                        _ppu.WriteRegisterByte(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0060 and <= 0x0400_00A8:
                        throw new NotImplementedException("Sound registers not yet implemented");
                    case uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE:
                        _dma.WriteByte(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0100 and <= 0x0400_0110:
                        return _timerController.WriteByte(address, value);
                    case uint _ when address is >= 0x0400_0120 and <= 0x0400_012C:
                        throw new NotImplementedException("Serial comms registers not yet implemented");
                    case uint _ when address is >= 0x0400_0130 and <= 0x0400_0132:
                        _gamepad.WriteByte(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0134 and <= 0x0400_015A:
                        throw new NotImplementedException("Serial comms registers not yet implemented");
                    case uint _ when address is >= 0x0400_0200 and <= 0x0470_0000:
                        return _interruptController.WriteByte(address, value);
                    default: 
                        throw new ArgumentOutOfRangeException(nameof(address), $"IO registers at address {address:X8} not mapped");
                };
            case uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF:
                _ppu.WriteByte(address, value);
                return 0;
            case uint _ when address is >= 0x0800_0000 and <= 0x0FFF_FFFF:
                return _gamePak.WriteByte(address, value);
            default:
                throw new ArgumentOutOfRangeException(nameof(address));
        }
    }

    internal int WriteHalfWord(uint address, ushort value)
    {
#if DEBUG
        _debugger.Log($"W {address:X8}={value:X4}");
#endif
        switch (address)
        {
            case uint _ when address <= 0x0000_3FFF:
                return 0;
            case uint _ when address is >= 0x0200_0000 and <= 0x0203_FFFF:
                Utils.WriteHalfWord(_onBoardWRam, 0x3_FFFF, address & 0x3_FFFF, value);
                return 2;
            case uint _ when address is >= 0x0300_0000 and <= 0x0300_7FFF:
                Utils.WriteHalfWord(_onChipWRam, 0x7FFF, address & 0x7FFF, value);
                return 0;
            case uint _ when address is >= 0x0400_0000 and <= 0x0400_03FE:
                switch (address)
                {
                    case uint _ when address is >= 0x0400_0000 and <= 0x0400_0056:
                        _ppu.WriteRegisterHalfWord(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0060 and <= 0x0400_00A8:
                        throw new NotImplementedException("Sound registers not yet implemented");
                    case uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE:
                        _dma.WriteHalfWord(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0100 and <= 0x0400_0110:
                        return _timerController.WriteHalfWord(address, value);
                    case uint _ when address is >= 0x0400_0120 and <= 0x0400_012C:
                        throw new NotImplementedException("Serial comms registers not yet implemented");
                    case uint _ when address is >= 0x0400_0130 and <= 0x0400_0132:
                        _gamepad.WriteHalfWord(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0134 and <= 0x0400_015A:
                        throw new NotImplementedException("Serial comms registers not yet implemented");
                    case uint _ when address is >= 0x0400_0200 and <= 0x0470_0000:
                        return _interruptController.WriteHalfWord(address, value);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(address), $"IO registers at address {address:X8} not mapped");
                };
            case uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF:
                _ppu.WriteHalfWord(address, value);
                return 0;
            case uint _ when address is >= 0x0800_0000 and <= 0x0FFF_FFFF:
                return _gamePak.WriteHalfWord(address, value);
            default:
                throw new ArgumentOutOfRangeException(nameof(address));
        }
    }

    internal int WriteWord(uint address, uint value)
    {
#if DEBUG
        _debugger.Log($"W {address:X8}={value:X8}");
#endif
        switch (address)
        {
            case uint _ when address <= 0x0000_3FFF:
                return 0;
            case uint _ when address is >= 0x0200_0000 and <= 0x0203_FFFF:
                Utils.WriteWord(_onBoardWRam, 0x3_FFFF, address & 0x3_FFFF, value);
                return 5; // TODO - 16 bit bus so this is a bit off
            case uint _ when address is >= 0x0300_0000 and <= 0x0300_7FFF:
                Utils.WriteWord(_onChipWRam, 0x7FFF, address & 0x7FFF, value);
                return 0;
            case uint _ when address is >= 0x0400_0000 and <= 0x0400_03FE:
                switch (address)
                {
                    case uint _ when address is >= 0x0400_0000 and <= 0x0400_0056:
                        _ppu.WriteRegisterHalfWord(address, (ushort)value);
                        _ppu.WriteRegisterHalfWord(address + 2, (ushort)(value >> 16));
                        return 1;
                    case uint _ when address is >= 0x0400_0060 and <= 0x0400_00A8:
                        throw new NotImplementedException("Sound registers not yet implemented");
                    case uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE:
                        _dma.WriteWord(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0100 and <= 0x0400_0110:
                        return _timerController.WriteWord(address, value);
                    case uint _ when address is >= 0x0400_0120 and <= 0x0400_012C:
                        throw new NotImplementedException("Serial comms registers not yet implemented");
                    case uint _ when address is >= 0x0400_0130 and <= 0x0400_0132:
                        _gamepad.WriteHalfWord(address, (ushort)value);
                        _gamepad.WriteHalfWord(address + 2, (ushort)(value >> 16));
                        return 0;
                    case uint _ when address is >= 0x0400_0134 and <= 0x0400_015A:
                        throw new NotImplementedException("Serial comms registers not yet implemented");
                    case uint _ when address is >= 0x0400_0200 and <= 0x0470_0000:
                        return _interruptController.WriteWord(address, value);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(address), $"IO registers at address {address:X8} not mapped");
                };
            case uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF:
                _ppu.WriteHalfWord(address, (ushort)value);
                _ppu.WriteHalfWord(address + 2, (ushort)(value >> 16));
                return 1;
            case uint _ when address is >= 0x0800_0000 and <= 0x0FFF_FFFF:
                return _gamePak.WriteWord(address, value);
            default:
                throw new ArgumentOutOfRangeException(nameof(address));
        }
    }
}