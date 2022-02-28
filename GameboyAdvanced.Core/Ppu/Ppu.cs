using static GameboyAdvanced.Core.IORegs;

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
    private ushort _currentLine;
    private readonly Background[] _backgrounds = new Background[4] { new Background(0), new Background(1), new Background(2), new Background(3) };

    private WindowControl _winIn = new();
    private WindowControl _winOut = new();
    private readonly Window[] _windows = new Window[2] { new Window(0), new Window(1) };

    private Mosaic _mosaic = new();

    private int _currentLineCycles;

    internal void Reset()
    {
        Array.Clear(_paletteRam);
        Array.Clear(_vram);
        Array.Clear(_oam);
        Array.Clear(_frameBuffer);
        _dispcnt = new DisplayCtrl();
        _greenSwap = 0;
        _dispstat = new GeneralLcdStatus();
        _currentLine = 0;
        _currentLineCycles = 0;
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
        if (_dispcnt.BgMode == BgMode.Video0)
        {
            foreach (var background in _backgrounds)
            {
                if ((_dispcnt.ScreenDisplayBg0 && background.Index == 0) ||
                    (_dispcnt.ScreenDisplayBg1 && background.Index == 1) ||
                    (_dispcnt.ScreenDisplayBg2 && background.Index == 2) ||
                    (_dispcnt.ScreenDisplayBg3 && background.Index == 3))
                {
                    // TODO - Mode 0 only implemented in so far as required for Deadbody cpu tests
                    // 32*32 tiles, 4 bit color depth, no flipping
                    var tileMapBase = background.Control.ScreenBaseBlock * 0x800;
                    var charMapBase = background.Control.CharBaseBlock * 0x4000;

                    for (var row = 0; row < 20; row++)
                    {
                        for (var col = 0; col < 30; col++)
                        {
                            var frameBufferTileAddress = (col * 8 * 4) + (row * Device.WIDTH * 4 * 8);
                            var tileMapAddress = tileMapBase + (row * 64) + (col * 2);
                            var tileMap = _vram[tileMapAddress] | (_vram[tileMapAddress + 1] << 8);
                            var tile = tileMap & 0b11_1111_1111;
                            var _horizontalFlip = ((tileMap >> 10) & 1) == 1;
                            var _verticalFlip = ((tileMap >> 11) & 1) == 1;
                            var paletteNumber = ((tileMap >> 12) & 0b1111) << 4;

                            var tileAddress = charMapBase + (tile * 32);
                            for (var b = 0; b < 32; b++)
                            {
                                var x = b % 4;
                                var y = b / 4;
                                var tileData = _vram[tileAddress + b];
                                var pixel1PalIx = tileData & 0b1111;
                                var pixel2PalIx = tileData >> 4;

                                // Color 0 in each BG/OBJ palette is transparent so replace with palette 0 color 0
                                var pixel1PalNo = (pixel1PalIx == 0) ? 0 : (paletteNumber | pixel1PalIx) * 2;
                                var pixel2PalNo = (pixel2PalIx == 0) ? 0 : (paletteNumber | pixel2PalIx) * 2;

                                // Each palette takes up 2
                                var pixel1Color = _paletteRam[pixel1PalNo] | (_paletteRam[pixel1PalNo + 1] << 8);
                                var pixel2Color = _paletteRam[pixel2PalNo] | (_paletteRam[pixel2PalNo + 1] << 8);
                                var fbPtr = frameBufferTileAddress + (x * 8) + (y * Device.WIDTH * 4);
                                Utils.ColorToRgb(pixel1Color, _frameBuffer.AsSpan(fbPtr));
                                Utils.ColorToRgb(pixel2Color, _frameBuffer.AsSpan(fbPtr + 4));
                            }
                        }
                    }
                }
            }
        }
        // TODO - BG mode 1-2
        else if (_dispcnt.BgMode == BgMode.Video3)
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
            var baseAddress = _dispcnt.Frame1Select ? 0x0000_A000 : 0x0;

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
    /// Step the PPU by a single master clock cycle
    /// </summary>
    internal void Step()
    {
        // TODO - Implement PPU properly
        _currentLineCycles++;

        if (_currentLineCycles == CyclesPerVisibleLine)
        {
            _dispstat.HBlankFlag = true;
        }
        else if (_currentLineCycles == CyclesPerLine)
        {
            _dispstat.HBlankFlag = false;
            _currentLine++;
            _currentLineCycles = 0;

            if (_currentLine == Device.HEIGHT)
            {
                _dispstat.VBlankFlag = true;
            }
            else if (_currentLine == VBlankLines + Device.HEIGHT)
            {
                _dispstat.VBlankFlag = false;
                _currentLine = 0;
            }
        }
    }

    internal void WriteRegisterByte(uint address, byte value) => throw new NotImplementedException("Writing byte wide values to PPU register not implemented");

    internal void WriteRegisterHalfWord(uint address, ushort value)
    {
        // TODO - Some of these writes won't be valid depending on the PPU state
        switch (address)
        {
            case DISPCNT:
                _dispcnt.Update(value);
                break;
            case GREENSWAP:
                _greenSwap = value;
                break;
            case DISPSTAT:
                _dispstat.Update(value);
                break;
            case BG0CNT:
                _backgrounds[0].Control.Update(value);
                break;
            case BG1CNT:
                _backgrounds[1].Control.Update(value);
                break;
            case BG2CNT:
                _backgrounds[2].Control.Update(value);
                break;
            case BG3CNT:
                _backgrounds[3].Control.Update(value);
                break;
            case BG0HOFS:
                _backgrounds[0].XOffset = value & 0x1FF;
                break;
            case BG0VOFS:
                _backgrounds[0].YOffset = value & 0x1FF;
                break;
            case BG1HOFS:
                _backgrounds[1].XOffset = value & 0x1FF;
                break;
            case BG1VOFS:
                _backgrounds[1].YOffset = value & 0x1FF;
                break;
            case BG2HOFS:
                _backgrounds[2].XOffset = value & 0x1FF;
                break;
            case BG2VOFS:
                _backgrounds[2].YOffset = value & 0x1FF;
                break;
            case BG3HOFS:
                _backgrounds[3].XOffset = value & 0x1FF;
                break;
            case BG3VOFS:
                _backgrounds[3].YOffset = value & 0x1FF;
                break;
            case BG2PA:
                _backgrounds[2].Dx = value;
                break;
            case BG2PB:
                _backgrounds[2].Dmx = value;
                break;
            case BG2PC:
                _backgrounds[2].Dy = value;
                break;
            case BG2PD:
                _backgrounds[2].Dmy = value;
                break;
            case BG2X:
                _backgrounds[2].RefPointX = (int)((_backgrounds[2].RefPointX & 0xFFFF_0000) | value);
                break;
            case BG2Y:
                _backgrounds[2].RefPointX = (_backgrounds[2].RefPointX & 0x0000_FFFF) | (value << 16);
                _backgrounds[2].RefPointX = (_backgrounds[2].RefPointX << 4) >> 4;
                break;
            case BG3PA:
                _backgrounds[3].Dx = value;
                break;
            case BG3PB:
                _backgrounds[3].Dmx = value;
                break;
            case BG3PC:
                _backgrounds[3].Dy = value;
                break;
            case BG3PD:
                _backgrounds[3].Dmy = value;
                break;
            case BG3X:
                _backgrounds[3].RefPointX = (int)((_backgrounds[3].RefPointX & 0xFFFF_0000) | value);
                break;
            case BG3Y:
                _backgrounds[3].RefPointX = (_backgrounds[3].RefPointX & 0x0000_FFFF) | (value << 16);
                _backgrounds[3].RefPointX = (_backgrounds[3].RefPointX << 4) >> 4;
                break;
            case WIN0H:
                _windows[0].X1 = value >> 8;
                _windows[0].X2 = (value & 0xFF);
                break;
            case WIN1H:
                _windows[1].X1 = value >> 8;
                _windows[1].X2 = (value & 0xFF);
                break;
            case WIN0V:
                _windows[0].Y1 = value >> 8;
                _windows[0].Y2 = (value & 0xFF);
                break;
            case WIN1V:
                _windows[1].Y1 = value >> 8;
                _windows[1].Y2 = (value & 0xFF);
                break;
            case WININ:
                _winIn.Set(value);
                break;
            case WINOUT:
                _winOut.Set(value);
                break;
            case MOSAIC:
                _mosaic.Set(value);
                break;
            case BLDCNT:
                throw new NotImplementedException("BLDCNT not yet implemented");
            case BLDALPHA:
                throw new NotImplementedException("BLDALPHA not yet implemented");
            case BLDY:
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
        DISPCNT => _dispcnt.Read(),
        GREENSWAP => _greenSwap,
        DISPSTAT => _dispstat.Read(),
        VCOUNT => _currentLine,
        BG0CNT => _backgrounds[0].Control.Read(),
        BG1CNT => _backgrounds[1].Control.Read(),
        BG2CNT => _backgrounds[2].Control.Read(),
        BG3CNT => _backgrounds[3].Control.Read(),
        WININ => _winIn.Get(),
        WINOUT => _winOut.Get(),
        BLDCNT => throw new NotImplementedException("BLDCNT register not implemented"),
        BLDALPHA => throw new NotImplementedException("BLDALPHA register not implemented"),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Unmapped PPU register read at {address:X8}") // TODO - Handle unused addresses properly
    };

    #region Memory Read Write

    internal byte ReadByte(uint address) => address switch
    {
        // TODO - Cycle timing needs to include extra cycle if ppu is accessing relevant memory area on this cycle
        >= 0x0500_0000 and <= 0x05FF_FFFF => _paletteRam[address & 0x3FF],
        >= 0x0600_0000 and <= 0x06FF_FFFF => _vram[address & 0x1_7FFF],
        >= 0x0700_0000 and <= 0x07FF_FFFF => _oam[address & 0x3F],
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused") // TODO - Handle unused addresses properly
    };

    internal ushort ReadHalfWord(uint address) => address switch
    {
        // TODO - Cycle timing needs to include extra cycle if ppu is accessing relevant memory area on this cycle
        >= 0x0500_0000 and <= 0x05FF_FFFF => Utils.ReadHalfWord(_paletteRam, address, 0x3FF),
        >= 0x0600_0000 and <= 0x06FF_FFFF => Utils.ReadHalfWord(_vram, address, 0x1_7FFF),
        >= 0x0700_0000 and <= 0x07FF_FFFF => Utils.ReadHalfWord(_oam, address, 0x3FF),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused") // TODO - Handle unused addresses properly
    };

    /// <summary>
    /// The PPU has a 16 bit bus and any byte wide writes to it result in half 
    /// word writes of the byte value to both bytes in the half word or the 
    /// write being ignored (depending on where exactly the write occurs)
    /// </summary>
    internal void WriteByte(uint address, byte value)
    {
        var hwAddress = address & 0xFFFF_FFFE;
        var hwValue = (ushort)((value << 8) | value);
        WriteHalfWord(hwAddress, hwValue);
    }

    internal void WriteHalfWord(uint address, ushort value)
    {
        // TODO - "VRAM and Palette RAM may be accessed during H-Blanking. OAM can accessed only if "H-Blank Interval Free" bit in DISPCNT register is set."
        switch (address)
        {
            case uint _ when address is >= 0x0500_0000 and <= 0x05FF_FFFF:
                Utils.WriteHalfWord(_paletteRam, 0x3FF, address, value);
                break;
            case uint _ when address is >= 0x0600_0000 and <= 0x06FF_FFFF:
                Utils.WriteHalfWord(_vram, 0x1_FFFF, address, value);
                break;
            case uint _ when address is >= 0x0700_0000 and <= 0x07FF_FFFF:
                Utils.WriteHalfWord(_oam, 0x3FF, address, value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused"); // TODO - Handle unused addresses properly
        }
    }

    #endregion
}
