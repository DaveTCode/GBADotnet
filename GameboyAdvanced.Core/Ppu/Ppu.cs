namespace GameboyAdvanced.Core.Ppu;

/// <summary>
/// This class both encapsulates the state of the PPU and provides 
/// functionality for rendering the current state into a bitmap
/// </summary>
internal class Ppu
{
    // TODO - Might be more efficient to store as ushorts given access is over 16 bit bus?
    private readonly byte[] _paletteRam = new byte[0x400]; // 1KB
    private readonly byte[] _vram = new byte[0x18000]; // 96KB
    private readonly byte[] _oam = new byte[0x400]; // 1KB
    private readonly byte[] _frameBuffer = new byte[Device.WIDTH * Device.HEIGHT * 4]; // RGBA order

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
    internal byte[] GetFrame()
    {
        if (_dispcnt.BgMode == BgMode.Video3)
        {
            // TODO - This just hacks up a return of the vram buffer from mode 3 -> RGB instead of processing per pixel
            for (var row = 0; row < 160; row++)
            {
                for (var col = 0; col < 240; col++)
                {
                    var vramPtr = 2 * ((row * 240) + col);
                    var fbPtr = 2 * vramPtr;
                    var hw = _vram[vramPtr] | (_vram[vramPtr + 1] << 8);
                    Utils.ColorToRgb(hw, _frameBuffer.AsSpan(fbPtr));
                }
            }
        }
        else if (_dispcnt.BgMode == BgMode.Video4)
        {
            // TODO - This just hacks up a return of the vram buffer from mode 4 -> palette -> RGB instead of processing per pixel
            for (var row = 0; row < 160; row++)
            {
                for (var col = 0; col < 240; col++)
                {
                    var vramPtr = ((row * 240) + col);
                    var fbPtr = 4 * vramPtr;
                    var paletteIndex = _vram[vramPtr] * 2; // 2 bytes per color in palette
                    var color = _paletteRam[paletteIndex] | (_paletteRam[paletteIndex + 1] << 8);
                    Utils.ColorToRgb(color, _frameBuffer.AsSpan(fbPtr));
                }
            }
        }

        return _frameBuffer;
    }

    /// <summary>
    /// Step the PPU by the specified number of cycles
    /// </summary>
    /// <param name="cycles"></param>
    internal void Step(int cycles)
    {
        // TODO - Implement PPU
    }

    internal void WriteRegisterByte(uint address, byte value) => throw new NotImplementedException("Writing byte wide values to PPU register not implemented");

    internal void WriteRegisterHalfWord(uint address, ushort value)
    {
        // TODO - Some of these writes won't be valid depending on the PPU state
        switch (address)
        {
            case 0x0400_0000:
                _dispcnt.Update(value);
                break;
            case 0x0400_0002:
                _greenSwap = value;
                break;
            case 0x0400_0004:
                _dispstat.Update(value);
                break;
            case 0x0400_0008:
                _bgCnt[0].Update(value);
                break;
            case 0x0400_000A:
                _bgCnt[1].Update(value);
                break;
            case 0x0400_000C:
                _bgCnt[2].Update(value);
                break;
            case 0x0400_000E:
                _bgCnt[3].Update(value);
                break;
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

    internal byte ReadRegisterByte(uint address) => address switch
    {
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Can't read single bytes from PPU registers at {address:X8}") // TODO - Handle unused addresses properly
    };

    internal ushort ReadRegisterHalfWord(uint address) => address switch
    {
        0x0400_0000 => _dispcnt.Read(),
        0x0400_0002 => _greenSwap,
        0x0400_0004 => _dispstat.Read(),
        0x0400_0006 => _verticalCounter,
        0x0400_0008 => _bgCnt[0].Read(),
        0x0400_000A => _bgCnt[1].Read(),
        0x0400_000C => _bgCnt[2].Read(),
        0x0400_000E => _bgCnt[3].Read(),
        0x0400_0048 => throw new NotImplementedException("WININ register not implemented"),
        0x0400_004A => throw new NotImplementedException("WINOUT register not implemented"),
        0x0400_0050 => throw new NotImplementedException("BLDCNT register not implemented"),
        0x0400_0052 => throw new NotImplementedException("BLDALPHA register not implemented"),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Unmapped PPU register read at {address:X8}") // TODO - Handle unused addresses properly
    };

    #region Memory Read Write

    internal byte ReadByte(uint address) => address switch
    {
        // TODO - Cycle timing needs to include extra cycle if ppu is accessing relevant memory area on this cycle
        >= 0x0500_0000 and <= 0x0500_03FF => _paletteRam[address & 0b0011_1111_1111],
        >= 0x0600_0000 and <= 0x0601_7FFF => _vram[address - 0x0600_0000],
        >= 0x0700_0000 and <= 0x0700_03FF => _oam[address & 0b0011_1111_1111],
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused") // TODO - Handle unused addresses properly
    };

    internal ushort ReadHalfWord(uint address) => address switch
    {
        // TODO - Cycle timing needs to include extra cycle if ppu is accessing relevant memory area on this cycle
        >= 0x0500_0000 and <= 0x0500_03FF => Utils.ReadHalfWord(_paletteRam, address, 0b0011_1111_1111),
        >= 0x0600_0000 and <= 0x0601_7FFF => Utils.ReadHalfWord(_vram, address - 0x0600_0000, 0xF_FFFF),
        >= 0x0700_0000 and <= 0x0700_03FF => Utils.ReadHalfWord(_oam, address, 0b0011_1111_1111),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused") // TODO - Handle unused addresses properly
    };

    /// <summary>
    /// The PPU has a 16 bit bus and any byte wide writes to it result in half 
    /// word writes of the byte value to both bytes in the half word.
    /// </summary>
    internal void WriteByte(uint address, byte value)
    {
        var hwAddress = address & ~1u;
        var hwValue = (ushort)((value << 8) | value);
        WriteHalfWord(hwAddress, hwValue);
    }

    internal void WriteHalfWord(uint address, ushort value)
    {
        switch (address)
        {
            case uint _ when address is >= 0x0500_0000 and <= 0x0500_03FF:
                Utils.WriteHalfWord(_paletteRam, 0x3FF, address & 0x3FF, value);
                break;
            case uint _ when address is >= 0x0600_0000 and <= 0x0601_7FFF:
                Utils.WriteHalfWord(_vram, 0x1_FFFF, address - 0x0600_0000, value);
                break;
            case uint _ when address is >= 0x0700_0000 and <= 0x0700_03FF:
                Utils.WriteHalfWord(_oam, 0x3FF, address & 0x3FF, value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused"); // TODO - Handle unused addresses properly
        }
    }

    #endregion
}
