namespace GameboyAdvanced.Core.Ppu;

/// <summary>
/// This class both encapsulates the state of the PPU and provides 
/// functionality for rendering the current state into a bitmap
/// </summary>
internal class Ppu
{
    private const int CyclesPerDot = 4;
    private const int CyclesPerVisibleLine = CyclesPerDot * Device.WIDTH; // 960
    private const int HBlankDots = 68;
    private const int CyclesPerHBlank = CyclesPerDot * HBlankDots; // 272
    private const int CyclesPerLine = CyclesPerVisibleLine + CyclesPerHBlank; // 1232
    private const int VBlankLines = 68;
    private const int VisibleLineCycles = Device.HEIGHT * CyclesPerLine; // 197,120
    private const int VBlankCycles = VBlankLines * CyclesPerLine; // 83,776
    private const int FrameCycles = VisibleLineCycles + VBlankCycles; // 280,896

    // TODO - Might be more efficient to store as ushorts given access is over 16 bit bus?
    private readonly byte[] _paletteRam = new byte[0x400]; // 1KB
    private readonly byte[] _vram = new byte[0x18000]; // 96KB
    private readonly byte[] _oam = new byte[0x400]; // 1KB
    private readonly byte[] _frameBuffer = new byte[Device.WIDTH * Device.HEIGHT * 4]; // RGBA order

    private DisplayCtrl _dispcnt = new();
    private ushort _greenSwap;
    private GeneralLcdStatus _dispstat = new();
    private ushort _verticalCounter;
    private readonly Background[] _backgrounds = new Background[4] { new Background(0), new Background(1), new Background(2), new Background(3) };

    private WindowControl _winIn = new();
    private WindowControl _winOut = new();
    private readonly Window[] _windows = new Window[2] { new Window(0), new Window(1) };

    private Mosaic _mosaic = new();

    private int _currentFrameCycles;

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
        _winIn = new WindowControl();
        _winOut = new WindowControl();
        _mosaic = new Mosaic();
        foreach (var bg in _backgrounds)
        {
            bg.Reset();
        }
        foreach (var window in _windows)
        {
            window.Reset();
        }
    }

    /// <summary>
    /// Returns the current state of the frame buffer and therefore should only
    /// be called when the buffer is fully complete (i.e. during vblank)
    /// </summary>
    internal byte[] GetFrame()
    {
        // TODO - BG mode 0-2
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
            var baseAddress = _dispcnt.Frame1Select ? 0x0600A000 : 0x0;

            // TODO - This just hacks up a return of the vram buffer from mode 4 -> palette -> RGB instead of processing per pixel
            for (var row = 0; row < 160; row++)
            {
                for (var col = 0; col < 240; col++)
                {
                    var vramPtr = ((row * 240) + col);
                    var fbPtr = 4 * vramPtr;
                    var paletteIndex = _vram[baseAddress + vramPtr] * 2; // 2 bytes per color in palette
                    var color = _paletteRam[paletteIndex] | (_paletteRam[paletteIndex + 1] << 8);
                    Utils.ColorToRgb(color, _frameBuffer.AsSpan(fbPtr));
                }
            }
        }
        else if (_dispcnt.BgMode == BgMode.Video5)
        {
            throw new NotImplementedException("BG Mode 5 not implemented");
        }

        return _frameBuffer;
    }

    /// <summary>
    /// Step the PPU by the specified number of cycles
    /// </summary>
    internal void Step(int cycles)
    {
        // TODO - Implement PPU
        _currentFrameCycles += cycles;
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
                _backgrounds[0].Control.Update(value);
                break;
            case 0x0400_000A:
                _backgrounds[1].Control.Update(value);
                break;
            case 0x0400_000C:
                _backgrounds[2].Control.Update(value);
                break;
            case 0x0400_000E:
                _backgrounds[3].Control.Update(value);
                break;
            case 0x0400_0010:
                _backgrounds[0].XOffset = value & 0x1FF;
                break;
            case 0x0400_0012:
                _backgrounds[0].YOffset = value & 0x1FF;
                break;
            case 0x0400_0014:
                _backgrounds[1].XOffset = value & 0x1FF;
                break;
            case 0x0400_0016:
                _backgrounds[1].YOffset = value & 0x1FF;
                break;
            case 0x0400_0018:
                _backgrounds[2].XOffset = value & 0x1FF;
                break;
            case 0x0400_001A:
                _backgrounds[2].YOffset = value & 0x1FF;
                break;
            case 0x0400_001C:
                _backgrounds[3].XOffset = value & 0x1FF;
                break;
            case 0x0400_001E:
                _backgrounds[3].YOffset = value & 0x1FF;
                break;
            case 0x0400_0020:
                _backgrounds[2].Dx = value;
                break;
            case 0x0400_0022:
                _backgrounds[2].Dmx = value;
                break;
            case 0x0400_0024:
                _backgrounds[2].Dy = value;
                break;
            case 0x0400_0026:
                _backgrounds[2].Dmy = value;
                break;
            case 0x0400_0028:
                _backgrounds[2].RefPointX = (int)((_backgrounds[2].RefPointX & 0xFFFF_0000) | value);
                break;
            case 0x0400_002A:
                _backgrounds[2].RefPointX = (_backgrounds[2].RefPointX & 0x0000_FFFF) | (value << 16);
                _backgrounds[2].RefPointX = (_backgrounds[2].RefPointX << 4) >> 4;
                break;
            case 0x0400_0030:
                _backgrounds[3].Dx = value;
                break;
            case 0x0400_0032:
                _backgrounds[3].Dmx = value;
                break;
            case 0x0400_0034:
                _backgrounds[3].Dy = value;
                break;
            case 0x0400_0036:
                _backgrounds[3].Dmy = value;
                break;
            case 0x0400_0038:
                _backgrounds[3].RefPointX = (int)((_backgrounds[3].RefPointX & 0xFFFF_0000) | value);
                break;
            case 0x0400_003A:
                _backgrounds[3].RefPointX = (_backgrounds[3].RefPointX & 0x0000_FFFF) | (value << 16);
                _backgrounds[3].RefPointX = (_backgrounds[3].RefPointX << 4) >> 4;
                break;
            case 0x0400_0040: // WIN0H
                _windows[0].X1 = value >> 8;
                _windows[0].X2 = (value & 0xFF);
                break;
            case 0x0400_0042: // WIN1H
                _windows[1].X1 = value >> 8;
                _windows[1].X2 = (value & 0xFF);
                break;
            case 0x0400_0044: // WIN0V
                _windows[0].Y1 = value >> 8;
                _windows[0].Y2 = (value & 0xFF);
                break;
            case 0x0400_0046: // WIN1V
                _windows[1].Y1 = value >> 8;
                _windows[1].Y2 = (value & 0xFF);
                break;
            case 0x0400_0048: // WININ
                _winIn.Set(value);
                break;
            case 0x0400_004A: // WINOUT
                _winOut.Set(value);
                break;
            case 0x0400_004C: // MOSAIC
                _mosaic.Set(value);
                break;
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
        0x0400_0006 => (ushort)(_currentFrameCycles / CyclesPerLine),
        0x0400_0008 => _backgrounds[0].Control.Read(),
        0x0400_000A => _backgrounds[1].Control.Read(),
        0x0400_000C => _backgrounds[2].Control.Read(),
        0x0400_000E => _backgrounds[3].Control.Read(),
        0x0400_0048 => _winIn.Get(),
        0x0400_004A => _winOut.Get(),
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
        // TODO - "VRAM and Palette RAM may be accessed during H-Blanking. OAM can accessed only if "H-Blank Interval Free" bit in DISPCNT register is set."
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
