namespace GameboyAdvanced.Core.Ppu;

/// <summary>
/// This class both encapsulates the state of the PPU and provides 
/// functionality for rendering the current state into a bitmap
/// </summary>
internal class Ppu
{
    private readonly byte[] _paletteRam = new byte[0x400]; // 1KB
    private readonly byte[] _vram = new byte[0x18000]; // 96KB
    private readonly byte[] _oam = new byte[0x400]; // 1KB
    private readonly byte[] _frameBuffer = new byte[Device.WIDTH * Device.HEIGHT];

    private DisplayCtrl _dispcnt = new();
    private ushort _greenSwap;
    private GeneralLcdStatus _dispstat = new();
    private ushort _verticalCounter;
    private readonly BgControlReg[] _bgCnt = new BgControlReg[4] { new BgControlReg(), new BgControlReg(), new BgControlReg(), new BgControlReg() };

    internal void Reset()
    {
        Array.Clear(_paletteRam);
        Array.Clear(_vram);
        Array.Clear(_oam);
        Array.Clear(_frameBuffer);
        _dispcnt = new DisplayCtrl();
        _greenSwap = 0;
        _dispstat = new GeneralLcdStatus();
        _verticalCounter = 0;
        for (var ii = 0; ii < 4; ii++)
        {
            _bgCnt[ii] = new BgControlReg();
        }
    }

    /// <summary>
    /// Returns the current state of the frame buffer and therefore should only
    /// be called when the buffer is fully complete (i.e. during vblank)
    /// </summary>
    internal byte[] GetFrame() => _frameBuffer;

    /// <summary>
    /// Step the PPU by the specified number of cycles
    /// </summary>
    /// <param name="cycles"></param>
    internal void Step(int cycles)
    {
        // TODO - Implement PPU
    }

    internal int WriteRegisterByte(uint address, byte value) => throw new NotImplementedException("Writing byte wide values to PPU register not implemented");

    internal int WriteRegisterHalfWord(uint address, ushort value)
    {
        // TODO - Some of these writes won't be valid depending on the PPU state
        switch (address)
        {
            case 0x0400_0000:
                _dispcnt.Update(value);
                return 1;
            case 0x0400_0002:
                _greenSwap = value;
                return 1;
            case 0x0400_0004:
                _dispstat.Update(value);
                return 1;
            case 0x0400_0008:
                _bgCnt[0].Update(value);
                return 1;
            case 0x0400_000A:
                _bgCnt[1].Update(value);
                return 1;
            case 0x0400_000C:
                _bgCnt[2].Update(value);
                return 1;
            case 0x0400_000E:
                _bgCnt[3].Update(value);
                return 1;
            case 0x0400_0010:
                throw new NotImplementedException("BG0HOFS not yet implemented");
            case 0x0400_0012:
                throw new NotImplementedException("BG0VOFS not yet implemented");
            case 0x0400_0014:
                throw new NotImplementedException("BG1HOFS not yet implemented");
            case 0x0400_0016:
                throw new NotImplementedException("BG1VOFS not yet implemented");
            case 0x0400_0018:
                throw new NotImplementedException("BG2HOFS not yet implemented");
            case 0x0400_001A:
                throw new NotImplementedException("BG2VOFS not yet implemented");
            case 0x0400_001C:
                throw new NotImplementedException("BG3HOFS not yet implemented");
            case 0x0400_001E:
                throw new NotImplementedException("BG3VOFS not yet implemented");
            case 0x0400_0020:
                throw new NotImplementedException("BG2PA not yet implemented");
            case 0x0400_0022:
                throw new NotImplementedException("BG2PB not yet implemented");
            case 0x0400_0024:
                throw new NotImplementedException("BG2PC not yet implemented");
            case 0x0400_0026:
                throw new NotImplementedException("BG2PD not yet implemented");
            case 0x0400_0030:
                throw new NotImplementedException("BG3PA not yet implemented");
            case 0x0400_0032:
                throw new NotImplementedException("BG3PB not yet implemented");
            case 0x0400_0034:
                throw new NotImplementedException("BG3PC not yet implemented");
            case 0x0400_0036:
                throw new NotImplementedException("BG3PD not yet implemented");
            case 0x0400_0040:
                throw new NotImplementedException("WIN0H not yet implemented");
            case 0x0400_0042:
                throw new NotImplementedException("WIN1H not yet implemented");
            case 0x0400_0044:
                throw new NotImplementedException("WIN0V not yet implemented");
            case 0x0400_0046:
                throw new NotImplementedException("WIN1V not yet implemented");
            case 0x0400_0048:
                throw new NotImplementedException("WININ not yet implemented");
            case 0x0400_004A:
                throw new NotImplementedException("WINOUT not yet implemented");
            case 0x0400_004C:
                throw new NotImplementedException("MOSAIC not yet implemented");
            case 0x0400_0050:
                throw new NotImplementedException("BLDCNT not yet implemented");
            case 0x0400_0052:
                throw new NotImplementedException("BLDALPHA not yet implemented");
            case 0x0400_0054:
                throw new NotImplementedException("BLDY not yet implemented");
            default:
                throw new NotImplementedException($"Unregistered half word write to PPU registers {address:X8}={value:X4}");
        }
    }

    internal int WriteRegisterWord(uint address, uint value)
    {
        switch (address)
        {
            case 0x0400_0028:
                throw new NotImplementedException("BG2X write not implemented");
            case 0x0400_002C:
                throw new NotImplementedException("BG2Y write not implemented");
            case 0x0400_0038:
                throw new NotImplementedException("BG3X write not implemented");
            case 0x0400_003C:
                throw new NotImplementedException("BG3Y write not implemented");
            default:
                throw new NotImplementedException($"Unregistered word write to PPU registers {address:X8}={value:X4}");
        }
    }

    internal (byte, int) ReadRegisterByte(uint address) => address switch
    {
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Can't read single bytes from PPU registers at {address:X8}") // TODO - Handle unused addresses properly
    };

    internal (ushort, int) ReadRegisterHalfWord(uint address) => address switch
    {
        0x0400_0000 => (_dispcnt.Read(), 1),
        0x0400_0002 => (_greenSwap, 1),
        0x0400_0004 => (_dispstat.Read(), 1),
        0x0400_0006 => (_verticalCounter, 1),
        0x0400_0008 => (_bgCnt[0].Read(), 1),
        0x0400_000A => (_bgCnt[1].Read(), 1),
        0x0400_000C => (_bgCnt[2].Read(), 1),
        0x0400_000E => (_bgCnt[3].Read(), 1),
        0x0400_0048 => throw new NotImplementedException("WININ register not implemented"),
        0x0400_004A => throw new NotImplementedException("WINOUT register not implemented"),
        0x0400_0050 => throw new NotImplementedException("BLDCNT register not implemented"),
        0x0400_0052 => throw new NotImplementedException("BLDALPHA register not implemented"),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Unmapped PPU register read at {address:X8}") // TODO - Handle unused addresses properly
    };

    internal (uint, int) ReadRegisterWord(uint address) => address switch
    {
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Can't read words from PPU registers at {address:X8}") // TODO - Handle unused addresses properly
    };

    #region Memory Read Write

    internal (byte, int) ReadByte(uint address) => address switch
    {
        // TODO - Cycle timing needs to include extra cycle if ppu is accessing relevant memory area on this cycle
        >= 0x0500_0000 and <= 0x0500_03FF => (_paletteRam[address & 0b0011_1111_1111], 0),
        >= 0x0600_0000 and <= 0x0601_7FFF => (_vram[address - 0x0600_0000], 0),
        >= 0x0700_0000 and <= 0x0700_03FF => (_oam[address & 0b0011_1111_1111], 0),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused") // TODO - Handle unused addresses properly
    };

    internal (ushort, int) ReadHalfWord(uint address) => address switch
    {
        // TODO - Cycle timing needs to include extra cycle if ppu is accessing relevant memory area on this cycle
        >= 0x0500_0000 and <= 0x0500_03FF => (Utils.ReadHalfWord(_paletteRam, address, 0b0011_1111_1111), 0),
        >= 0x0600_0000 and <= 0x0601_7FFF => (Utils.ReadHalfWord(_vram, address - 0x0600_0000, 0xF_FFFF), 0),
        >= 0x0700_0000 and <= 0x0700_03FF => (Utils.ReadHalfWord(_oam, address, 0b0011_1111_1111), 0),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused") // TODO - Handle unused addresses properly
    };

    internal (uint, int) ReadWord(uint address) => address switch
    {
        // TODO - Cycle timing needs to include extra cycle if ppu is accessing relevant memory area on this cycle
        >= 0x0500_0000 and <= 0x0500_03FF => (Utils.ReadWord(_paletteRam, address, 0b0011_1111_1111), 0),
        >= 0x0600_0000 and <= 0x0601_7FFF => (Utils.ReadWord(_vram, address - 0x0600_0000, 0xF_FFFF), 0),
        >= 0x0700_0000 and <= 0x0700_03FF => (Utils.ReadWord(_oam, address, 0b0011_1111_1111), 0),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused") // TODO - Handle unused addresses properly
    };

    internal int WriteByte(uint address, byte value)
    {
        switch (address)
        {
            case uint _ when address is >= 0x0500_0000 and <= 0x0500_03FF:
                _paletteRam[address & 0b0011_1111_1111] = value;
                return 0;
            case uint _ when address is >= 0x0600_0000 and <= 0x0601_7FFF:
                _vram[address - 0x0600_0000] = value;
                return 0;
            case uint _ when address is >= 0x0700_0000 and <= 0x0700_03FF:
                _oam[address & 0b0011_1111_1111] = value;
                return 0;
            default:
                throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused"); // TODO - Handle unused addresses properly
        }
    }

    internal int WriteHalfWord(uint address, ushort value)
    {
        switch (address)
        {
            case uint _ when address is >= 0x0500_0000 and <= 0x0500_03FF:
                Utils.WriteHalfWord(_paletteRam, 0x3FF, address & 0x3FF, value);
                return 0;
            case uint _ when address is >= 0x0600_0000 and <= 0x0601_7FFF:
                Utils.WriteHalfWord(_vram, 0x3FF, address - 0x0600_0000, value);
                return 0;
            case uint _ when address is >= 0x0700_0000 and <= 0x0700_03FF:
                Utils.WriteHalfWord(_oam, 0x3FF, address & 0x3FF, value);
                return 0;
            default:
                throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused"); // TODO - Handle unused addresses properly
        }
    }

    internal int WriteWord(uint address, uint value)
    {
        switch (address)
        {
            case uint _ when address is >= 0x0500_0000 and <= 0x0500_03FF:
                Utils.WriteWord(_paletteRam, 0x3FF, address & 0x3FF, value);
                return 0;
            case uint _ when address is >= 0x0600_0000 and <= 0x0601_7FFF:
                Utils.WriteWord(_vram, 0x3FF, address - 0x0600_0000, value);
                return 0;
            case uint _ when address is >= 0x0700_0000 and <= 0x0700_03FF:
                Utils.WriteWord(_oam, 0x3FF, address & 0x3FF, value);
                return 0;
            default:
                throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused"); // TODO - Handle unused addresses properly
        }
    }

    #endregion
}
