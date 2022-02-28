﻿using static GameboyAdvanced.Core.IORegs;
using GameboyAdvanced.Core.Cpu.Interrupts;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Dma;
using GameboyAdvanced.Core.Input;
using GameboyAdvanced.Core.Rom;
using GameboyAdvanced.Core.Serial;
using GameboyAdvanced.Core.Timer;

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
    private readonly InterruptWaitStateAndPowerControlRegisters _interruptController;
    private readonly SerialController _serialController;
    private readonly byte[] _bios = new byte[0x4000];
    private readonly byte[] _onBoardWRam = new byte[0x4_0000];
    private readonly byte[] _onChipWRam = new byte[0x8000];
    private WaitControl _waitControl;

    internal void Reset()
    {
        Array.Clear(_onBoardWRam);
        Array.Clear(_onChipWRam);
        _waitControl.Reset();
    }

    internal MemoryBus(
        byte[] bios,
        Gamepad gamepad,
        GamePak gamePak,
        Ppu.Ppu ppu,
        DmaDataUnit dma,
        TimerController timerController,
        InterruptWaitStateAndPowerControlRegisters interruptController,
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
        _interruptController = interruptController ?? throw new ArgumentNullException(nameof(interruptController));
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _serialController = serialController ?? throw new ArgumentNullException(nameof(serialController));
        _waitControl = new WaitControl();
    }

    internal (byte, int) ReadByte(uint address, int seq)
    {
        var (val, waitStates) = address switch
        {
            uint _ when address <= 0x0000_3FFF => (_bios[address], 0), // TODO - Can only read from bios when IP is located in BIOS region
            uint _ when address is >= 0x0200_0000 and <= 0x02FF_FFFF => (_onBoardWRam[address & 0x3_FFFF], 2),
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
                uint _ when address is >= 0x0400_0200 and <= 0x0470_0000 => (_interruptController.ReadByte(address), 0),
                _ => throw new ArgumentOutOfRangeException(nameof(address), $"IO registers at {address:X8} not mapped"),
            },
            uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF => (_ppu.ReadByte(address), 0),
            uint _ when address is >= 0x0800_0000 and <= 0x09FF_FFFF => (_gamePak.ReadByte(address), _waitControl.WaitState0[seq]),
            uint _ when address is >= 0x0A00_0000 and <= 0x0BFF_FFFF => (_gamePak.ReadByte(address), _waitControl.WaitState1[seq]),
            uint _ when address is >= 0x0C00_0000 and <= 0x0DFF_FFFF => (_gamePak.ReadByte(address), _waitControl.WaitState2[seq]),
            uint _ when address is >= 0x0E00_0000 and <= 0x0FFF_FFFF => (_gamePak.ReadSRam(address), _waitControl.SRAMWaitControl),
            _ => ((byte)0, 0), // TODO - Hacking for mgba test suite throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} not memory mapped")
        };

#if DEBUG
        _debugger.Log($"R {address:X8}={val:X2}");
#endif
        return (val, waitStates);
    }

    internal (ushort, int) ReadHalfWord(uint address, int seq)
    {
        var (val, waitStates) = address switch
        {
            uint a when a <= 0x0000_3FFF => (Utils.ReadHalfWord(_bios, address, 0x3FFF), 0), // TODO - Can only read from bios when IP is located in BIOS region
            uint a when a is >= 0x0200_0000 and <= 0x02FF_FFFF => (Utils.ReadHalfWord(_onBoardWRam, address, 0x3_FFFF), 2),
            uint a when a is >= 0x0300_0000 and <= 0x03FF_FFFF => (Utils.ReadHalfWord(_onChipWRam, address, 0x7FFF), 0),
            uint a when a is >= 0x0400_0000 and <= 0x0400_03FE => address switch
            {
                uint _ when address is >= 0x0400_0000 and <= 0x0400_0056 => (_ppu.ReadRegisterHalfWord(address), 0),
                uint _ when address is >= 0x0400_0060 and <= 0x0400_00A8 => ((ushort)0, 0), // TODO - Sound registers,
                uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE => (_dma.ReadHalfWord(address), 0),
                uint _ when address is >= 0x0400_0100 and <= 0x0400_0110 => (_timerController.ReadHalfWord(address), 0),
                uint _ when address is >= 0x0400_0120 and <= 0x0400_012C => (_serialController.ReadHalfWord(address), 0),
                uint _ when address is >= 0x0400_0130 and <= 0x0400_0132 => (_gamepad.ReadHalfWord(address), 0),
                uint _ when address is >= 0x0400_0134 and <= 0x0400_015A => (_serialController.ReadHalfWord(address), 0),
                WAITCNT => (_waitControl.Get(), 0),
                uint _ when address is >= 0x0400_0200 and <= 0x0470_0000 => (_interruptController.ReadHalfWord(address), 0),
                _ => throw new ArgumentOutOfRangeException(nameof(address), $"IO registers at {address:X8} not mapped"),
            },
            uint a when a is >= 0x0500_0000 and <= 0x07FF_FFFF => (_ppu.ReadHalfWord(address), 0),
            uint _ when address is >= 0x0800_0000 and <= 0x09FF_FFFF => (_gamePak.ReadHalfWord(address), _waitControl.WaitState0[seq]),
            uint _ when address is >= 0x0A00_0000 and <= 0x0BFF_FFFF => (_gamePak.ReadHalfWord(address), _waitControl.WaitState1[seq]),
            uint _ when address is >= 0x0C00_0000 and <= 0x0DFF_FFFF => (_gamePak.ReadHalfWord(address), _waitControl.WaitState2[seq]),
            uint _ when address is >= 0x0E00_0000 and <= 0x0FFF_FFFF => ((ushort)(_gamePak.ReadSRam(address) * 0x0101), _waitControl.SRAMWaitControl),
            _ => ((ushort)0, 0), // TODO - Hacking for mgba test suite throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} not memory mapped")
        };

#if DEBUG
        _debugger.Log($"R {address:X8}={val:X4}");
#endif
        return (val, waitStates);
    }

    internal (uint, int) ReadWord(uint address, int seq)
    {
        if (address == 0x080004D0)
        {
            var a = 1;
        }
        var (val, waitStates) = address switch
        {
            uint a when a <= 0x0000_3FFF => (Utils.ReadWord(_bios, address, 0x3FFF), 0), // TODO - Can only read from bios when IP is located in BIOS region
            uint a when a is >= 0x0200_0000 and <= 0x02FF_FFFF => (Utils.ReadWord(_onBoardWRam, address, 0x3_FFFF), 5),
            uint a when a is >= 0x0300_0000 and <= 0x03FF_FFFF => (Utils.ReadWord(_onChipWRam, address, 0x7FFF), 0),
            uint a when a is >= 0x0400_0000 and <= 0x0400_03FE => address switch
            {
                uint _ when address is >= 0x0400_0000 and <= 0x0400_0056 => ((uint)(_ppu.ReadRegisterHalfWord(address) | (_ppu.ReadRegisterHalfWord(address + 2) << 16)), 0),
                uint _ when address is >= 0x0400_0060 and <= 0x0400_00A8 => throw new NotImplementedException("Sound registers not yet implemented"),
                uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE => (_dma.ReadWord(address), 0),
                uint _ when address is >= 0x0400_0100 and <= 0x0400_0110 => (_timerController.ReadWord(address), 0),
                uint _ when address is >= 0x0400_0120 and <= 0x0400_012C => (_serialController.ReadWord(address), 0),
                uint _ when address is >= 0x0400_0130 and <= 0x0400_0132 => ((uint)(_gamepad.ReadHalfWord(address) | (_gamepad.ReadHalfWord(address + 2) << 16)), 0),
                uint _ when address is >= 0x0400_0134 and <= 0x0400_015A => (_serialController.ReadWord(address), 0),
                WAITCNT => (_waitControl.Get(), 0),
                uint _ when address is >= 0x0400_0200 and <= 0x0470_0000 => (_interruptController.ReadWord(address), 0),
                _ => throw new ArgumentOutOfRangeException(nameof(address), $"IO registers at {address:X8} not mapped"),
            },
            uint a when a is >= 0x0500_0000 and <= 0x07FF_FFFF => ((uint)(_ppu.ReadHalfWord(address) | (_ppu.ReadHalfWord(address + 2) << 16)), 1),
            uint _ when address is >= 0x0800_0000 and <= 0x09FF_FFFF => (_gamePak.ReadWord(address), _waitControl.WaitState0[seq] + _waitControl.WaitState0[1]),
            uint _ when address is >= 0x0A00_0000 and <= 0x0BFF_FFFF => (_gamePak.ReadWord(address), _waitControl.WaitState1[seq] + _waitControl.WaitState1[1]),
            uint _ when address is >= 0x0C00_0000 and <= 0x0DFF_FFFF => (_gamePak.ReadWord(address), _waitControl.WaitState2[seq] + _waitControl.WaitState2[1]),
            uint _ when address is >= 0x0E00_0000 and <= 0x0FFF_FFFF => (_gamePak.ReadSRam(address) * 0x01010101u, _waitControl.SRAMWaitControl),
            _ => (0u, 0), // TODO - Hacking for mgba test suite throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} not memory mapped")
        };

#if DEBUG
        _debugger.Log($"R {address:X8}={val:X8}");
#endif
        return (val, waitStates);
    }

    internal int WriteByte(uint address, byte value, int seq)
    {
#if DEBUG
        _debugger.Log($"W {address:X8}={value:X2}");
#endif
        switch (address)
        {
            case uint _ when address <= 0x0000_3FFF:
                return 0;
            case uint _ when address is >= 0x0200_0000 and <= 0x02FF_FFFF:
                _onBoardWRam[address & 0x3_FFFF] = value;
                return 2;
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
                    case uint _ when address is >= 0x0400_0200 and <= 0x0470_0000:
                        _interruptController.WriteByte(address, value);
                        return 0;
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

    internal int WriteHalfWord(uint address, ushort value, int seq)
    {
#if DEBUG
        _debugger.Log($"W {address:X8}={value:X4}");
#endif
        switch (address)
        {
            case uint _ when address <= 0x0000_3FFF:
                return 0;
            case uint _ when address is >= 0x0200_0000 and <= 0x03FF_FFFF:
                Utils.WriteHalfWord(_onBoardWRam, 0x3_FFFF, address, value);
                return 2;
            case uint _ when address is >= 0x0300_0000 and <= 0x04FF_FFFF:
                Utils.WriteHalfWord(_onChipWRam, 0x7FFF, address, value);
                return 0;
            case uint _ when address is >= 0x0400_0000 and <= 0x0400_03FE:
                switch (address)
                {
                    case uint _ when address is >= 0x0400_0000 and <= 0x0400_0056:
                        _ppu.WriteRegisterHalfWord(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0060 and <= 0x0400_00A8:
                        // TODO - No APU or sound registers yet
                        return 0;
                    case uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE:
                        _dma.WriteHalfWord(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0100 and <= 0x0400_0110:
                        _timerController.WriteHalfWord(address, value);
                        return 0;
                    case 0x0400_0114: // BIOS bug writes to this
                        return 0;
                    case uint _ when address is >= 0x0400_0120 and <= 0x0400_012C:
                        _serialController.WriteHalfWord(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0130 and <= 0x0400_0132:
                        _gamepad.WriteHalfWord(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0134 and <= 0x0400_015A:
                        _serialController.WriteHalfWord(address, value);
                        return 0;
                    case WAITCNT:
                        _waitControl.Set(value);
                        return 0;
                    case uint _ when address is >= 0x0400_0200 and <= 0x0470_0000:
                        _interruptController.WriteHalfWord(address, value);
                        return 0;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(address), $"IO registers at address {address:X8} not mapped");
                };
            case uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF:
                _ppu.WriteHalfWord(address, value);
                return 0;
            case uint _ when address is >= 0x0800_0000 and <= 0x0DFF_FFFF:
                return 0; // TODO - Is it right that no wait states occur on attempted writes to Gamepak?
            case uint _ when address is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                // SRAM bus is 8 bit, we take the rotated value of the HW to store (same as LDRH
                var rotate = 8 * (int)(address % 2);
                var rotatedVal = (value >> rotate) | (value << (32 - rotate));
                _gamePak.WriteSRam(address, (byte)rotatedVal);
                return _waitControl.SRAMWaitControl;
            default:
                return 0; // TODO - Just hacking this in for now because mgba test suite writes to 0x04FFF780 during startup
                // throw new ArgumentOutOfRangeException(nameof(address));
        }
    }

    internal int WriteWord(uint address, uint value, int seq)
    {
#if DEBUG
        _debugger.Log($"W {address:X8}={value:X8}");
#endif

        switch (address)
        {
            case uint _ when address <= 0x0000_3FFF:
                return 0;
            case uint _ when address is >= 0x0200_0000 and <= 0x03FF_FFFF:
                Utils.WriteWord(_onBoardWRam, 0x3_FFFF, address, value);
                return 5; // TODO - 16 bit bus so this is a bit off
            case uint _ when address is >= 0x0300_0000 and <= 0x04FF_FFFF:
                Utils.WriteWord(_onChipWRam, 0x7FFF, address, value);
                return 0;
            case uint _ when address is >= 0x0400_0000 and <= 0x0400_03FE:
                switch (address)
                {
                    case uint _ when address is >= 0x0400_0000 and <= 0x0400_0056:
                        _ppu.WriteRegisterHalfWord(address, (ushort)value);
                        _ppu.WriteRegisterHalfWord(address + 2, (ushort)(value >> 16));
                        return 0;
                    case uint _ when address is >= 0x0400_0060 and <= 0x0400_00A8:
                        // TODO - No APU or sound registers yet
                        return 0;
                    case uint _ when address is >= 0x0400_00B0 and <= 0x0400_00DE:
                        _dma.WriteWord(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0100 and <= 0x0400_0110:
                        _timerController.WriteWord(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0120 and <= 0x0400_012C:
                        _serialController.WriteWord(address, value);
                        return 0;
                    case uint _ when address is >= 0x0400_0130 and <= 0x0400_0132:
                        _gamepad.WriteHalfWord(address, (ushort)value);
                        _gamepad.WriteHalfWord(address + 2, (ushort)(value >> 16));
                        return 0;
                    case uint _ when address is >= 0x0400_0134 and <= 0x0400_015A:
                        _serialController.WriteWord(address, value);
                        return 0;
                    case WAITCNT:
                        _waitControl.Set((ushort)value);
                        // 206 is unused so no extra write here
                        return 0;
                    case uint _ when address is >= 0x0400_0200 and <= 0x0470_0000:
                        _interruptController.WriteWord(address, value);
                        return 0;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(address), $"IO registers at address {address:X8} not mapped");
                };
            case uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF:
                _ppu.WriteHalfWord(address, (ushort)value);
                _ppu.WriteHalfWord(address + 2, (ushort)(value >> 16));
                return 1;
            case uint _ when address is >= 0x0800_0000 and <= 0x0DFF_FFFF:
                return 0; // TODO - Is it right that no wait states occur on attempted writes to Gamepak?
            case uint _ when address is >= 0x0E00_0000 and <= 0x0FFF_FFFF:
                // SRAM bus is 8 bit, we take the rotated value of the W to store (same as LDR)
                var rotate = 8 * (int)(address % 4);
                var rotatedVal = (value >> rotate) | (value << (32 - rotate));
                _gamePak.WriteSRam(address, (byte)rotatedVal);
                return _waitControl.SRAMWaitControl;
            default:
                return 0; // TODO - Just hacking this in for now because mgba test suite writes to 0x04FFF780 during startup
                // throw new ArgumentOutOfRangeException(nameof(address));
        }
    }
}