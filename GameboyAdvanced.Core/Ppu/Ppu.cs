using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Interrupts;
using GameboyAdvanced.Core.Ppu.Registers;
using System.Runtime.CompilerServices;
using static GameboyAdvanced.Core.IORegs;

namespace GameboyAdvanced.Core.Ppu;

/// <summary>
/// This class both encapsulates the state of the PPU and provides 
/// functionality for rendering the current state into a bitmap
/// </summary>
internal partial class Ppu
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

    private readonly BaseDebugger _debugger;
    private readonly InterruptInterconnect _interruptInterconnect;

    // TODO - Might be more efficient to store as ushorts given access is over 16 bit bus?
    private readonly byte[] _vram = new byte[0x18000]; // 96KB
    private readonly byte[] _frameBuffer = new byte[Device.WIDTH * Device.HEIGHT * 4]; // RGBA order
    private readonly int[][] _scanlineBgBuffer = new int[4][];
    private readonly int[] _scanlinePriorities = new int[Device.WIDTH];

    private DisplayCtrl _dispcnt = new();
    private ushort _greenSwap;
    private GeneralLcdStatus _dispstat = new();
    private ushort _currentLine;
    private readonly Background[] _backgrounds = new Background[4] { new Background(0), new Background(1), new Background(2), new Background(3) };

    private WindowControl _winIn = new();
    private WindowControl _winOut = new();
    private readonly Window[] _windows = new Window[2] { new Window(0), new Window(1) };
    private Mosaic _mosaic = new();
    private Bldcnt _bldcnt = new();
    private BldAlpha _bldalpha = new();

    private int _currentLineCycles;

    internal Ppu(BaseDebugger debugger, InterruptInterconnect interruptInterconnect)
    {
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _interruptInterconnect = interruptInterconnect ?? throw new ArgumentNullException(nameof(interruptInterconnect));

        for (var ii = 0; ii < 4; ii++)
        {
            _scanlineBgBuffer[ii] = new int[Device.WIDTH];
        }

        for (var ii = 0; ii < _sprites.Length; ii++)
        {
            _sprites[ii].Index = ii;
        }
    }

    internal void Reset()
    {
        Array.Clear(_paletteRam);
        Array.Clear(_vram);
        Array.Clear(_oam);
        Array.Clear(_frameBuffer);
        _dispcnt.Reset();
        _greenSwap = 0;
        _dispstat = new GeneralLcdStatus();
        _currentLine = 0;
        _currentLineCycles = 0;
        _winIn = new WindowControl();
        _winOut = new WindowControl();
        _mosaic = new Mosaic();
        _bldcnt.Reset();
        _bldalpha.Reset();
        for (var ii = 0; ii < 4; ii++)
        {
            _backgrounds[ii].Reset();
            Array.Clear(_scanlineBgBuffer[ii]);
        }
        foreach (var window in _windows)
        {
            window.Reset();
        }

        foreach (var sprite in _sprites)
        {
            sprite.Reset();
        }
    }

    /// <summary>
    /// Returns the current state of the frame buffer and therefore should only
    /// be called when the buffer is fully complete (i.e. during vblank)
    /// </summary>
    internal byte[] GetFrame()
    {
        return _frameBuffer;
    }

    internal bool CanVBlankDma() => _dispstat.VBlankFlag;

    // TODO - Not sure what the behaviour here is if accessing OAM while the bit on dispcnt isn't set, does it pause DMA or write nothing?
    internal bool CanHBlankDma() => _dispstat.HBlankFlag;

    /// <summary>
    /// Step the PPU by a single master clock cycle.
    /// 
    /// Rendering currently happens at the start of hblank on a per scanline 
    /// basis so this function is mostly responsible for keeping track of
    /// hblank, vcount, vblank and raising interrupts on the right cycle.
    /// </summary>
    internal void Step()
    {
        _currentLineCycles++;

        if (_currentLineCycles == CyclesPerVisibleLine)
        {
            if (_currentLine < Device.HEIGHT)
            {
                DrawCurrentScanline();
            }
            _dispstat.HBlankFlag = true;
            if (_dispstat.HBlankIrqEnable)
            {
                _interruptInterconnect.RaiseInterrupt(Interrupt.LCDHBlank);
            }
        }
        else if (_currentLineCycles == CyclesPerLine)
        {
            _dispstat.VCounterFlag = false;
            _dispstat.HBlankFlag = false;
            _currentLine++;
            _currentLineCycles = 0;

            if (_currentLine == _dispstat.VCountSetting)
            {
                _dispstat.VCounterFlag = true;
                if (_dispstat.VCounterIrqEnable)
                {
                    _interruptInterconnect.RaiseInterrupt(Interrupt.LCDVCounter);
                }
            }

            if (_currentLine == Device.HEIGHT)
            {
                _dispstat.VBlankFlag = true;
                if (_dispstat.VBlankIrqEnable)
                {
                    _interruptInterconnect.RaiseInterrupt(Interrupt.LCDVBlank);
                }
            }
            else if (_currentLine == VBlankLines + Device.HEIGHT - 1)
            {
                _dispstat.VBlankFlag = false;
            }
            else if (_currentLine == VBlankLines + Device.HEIGHT)
            {
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
            case BG2X_L:
                _backgrounds[2].RefPointX = (int)((_backgrounds[2].RefPointX & 0xFFFF_0000) | value);
                break;
            case BG2X_H:
                _backgrounds[2].RefPointX = (int)((_backgrounds[2].RefPointX & 0x0000_FFFF) | (value << 16)); // TODO - Mask to 12 bits?
                _backgrounds[2].RefPointX = (_backgrounds[2].RefPointX << 4) >> 4;
                break;
            case BG2Y_L:
                _backgrounds[2].RefPointY = (int)((_backgrounds[2].RefPointY & 0xFFFF_0000) | value);
                break;
            case BG2Y_H:
                _backgrounds[2].RefPointY = (int)((_backgrounds[2].RefPointY & 0x0000_FFFF) | (value << 16)); // TODO - Mask to 12 bits?
                _backgrounds[2].RefPointY = (_backgrounds[2].RefPointY << 4) >> 4;
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
            case BG3X_L:
                _backgrounds[3].RefPointX = (int)((_backgrounds[3].RefPointX & 0xFFFF_0000) | value);
                break;
            case BG3X_H:
                _backgrounds[3].RefPointX = (int)((_backgrounds[3].RefPointX & 0x0000_FFFF) | (value << 16)); // TODO - Mask to 12 bits?
                _backgrounds[3].RefPointX = (_backgrounds[3].RefPointX << 4) >> 4;
                break;
            case BG3Y_L:
                _backgrounds[3].RefPointY = (int)((_backgrounds[3].RefPointY & 0xFFFF_0000) | value);
                break;
            case BG3Y_H:
                _backgrounds[3].RefPointY = (int)((_backgrounds[3].RefPointY & 0x0000_FFFF) | (value << 16)); // TODO - Mask to 12 bits?
                _backgrounds[3].RefPointY = (_backgrounds[3].RefPointY << 4) >> 4;
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
                _bldcnt.Set(value);
                break;
            case BLDALPHA:
                _bldalpha.Set(value);
                break;
            case BLDY:
                break; // TODO - Implement BLDY
            default:
                return; // TODO - Make sure behaviour on unknown writes is ok
        }
    }

    internal byte ReadRegisterByte(uint address, uint openbus) => (byte)ReadRegisterHalfWord(address, openbus); // TODO - Not 100% confident about this although beeg.gba does it and the result works in that specific case (read VCOUNT with high byte being 0)

    internal ushort ReadRegisterHalfWord(uint address, uint openbus) => address switch
    {
        DISPCNT => _dispcnt.Read(),
        GREENSWAP => _greenSwap,
        DISPSTAT => _dispstat.Read(),
        VCOUNT => _currentLine,
        BG0CNT => (ushort)(_backgrounds[0].Control.Read() & 0xDFFF),
        BG1CNT => (ushort)(_backgrounds[1].Control.Read() & 0xDFFF),
        BG2CNT => _backgrounds[2].Control.Read(),
        BG3CNT => _backgrounds[3].Control.Read(),
        WININ => _winIn.Get(),
        WINOUT => _winOut.Get(),
        BLDCNT => _bldcnt.Get(),
        BLDALPHA => _bldalpha.Get(),
        _ => (ushort)openbus,
    };

    #region Memory Read Write

    internal byte ReadByte(uint address) => address switch
    {
        // TODO - Cycle timing needs to include extra cycle if ppu is accessing relevant memory area on this cycle
        >= 0x0500_0000 and <= 0x05FF_FFFF => ReadPaletteByte(address),
        >= 0x0600_0000 and <= 0x06FF_FFFF => _vram[MaskVRamAddress(address)],
        >= 0x0700_0000 and <= 0x07FF_FFFF => ReadOamByte(address),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused") // TODO - Handle unused addresses properly
    };

    internal ushort ReadHalfWord(uint address) => address switch
    {
        // TODO - Cycle timing needs to include extra cycle if ppu is accessing relevant memory area on this cycle
        >= 0x0500_0000 and <= 0x05FF_FFFF => ReadPaletteHalfWord(address),
        >= 0x0600_0000 and <= 0x06FF_FFFF => Utils.ReadHalfWord(_vram, MaskVRamAddress(address), 0xF_FFFF),
        >= 0x0700_0000 and <= 0x07FF_FFFF => ReadOamHalfWord(address),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused") // TODO - Handle unused addresses properly
    };

    /// <summary>
    /// The PPU has a 16 bit bus and any byte wide writes to it result in half 
    /// word writes of the byte value to both bytes in the half word or the 
    /// write being ignored (depending on where exactly the write occurs)
    /// </summary>
    internal void WriteByte(uint address, byte value)
    {
        switch (address)
        {
            case uint _ when address is >= 0x0500_0000 and <= 0x05FF_FFFF:
                {
                    WritePaletteByte(address, value);
                    break;
                }
            case uint _ when address is >= 0x0600_0000 and <= 0x06FF_FFFF:
                {
                    var hwAddress = address & 0xFFFF_FFFE;
                    var maskedAddress = MaskVRamAddress(hwAddress);
                    // 8 bit writes to OBJ are ignored
                    if ((int)_dispcnt.BgMode >= 3 && maskedAddress >= 0x0001_4000)
                    {
                        break;
                    }
                    else if (maskedAddress >= 0x0001_0000)
                    {
                        break;
                    }
                    else
                    {
                        var hwValue = (ushort)((value << 8) | value);
                        Utils.WriteHalfWord(_vram, 0xF_FFFF, maskedAddress, hwValue);
                        break;
                    }
                }
            case uint _ when address is >= 0x0700_0000 and <= 0x07FF_FFFF:
                // 8 bit writes to OAM are ignored
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused");
        }
    }

    internal void WriteHalfWord(uint address, ushort value)
    {
        // TODO - "VRAM and Palette RAM may be accessed during H-Blanking. OAM can accessed only if "H-Blank Interval Free" bit in DISPCNT register is set."
        switch (address)
        {
            case uint _ when address is >= 0x0500_0000 and <= 0x05FF_FFFF:
                WritePaletteHalfWord(address, value);
                break;
            case uint _ when address is >= 0x0600_0000 and <= 0x06FF_FFFF:
                Utils.WriteHalfWord(_vram, 0x1_FFFF, MaskVRamAddress(address), value);
                break;
            case uint _ when address is >= 0x0700_0000 and <= 0x07FF_FFFF:
                WriteOamHalfWord(address, value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is unused"); // TODO - Handle unused addresses properly
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MaskVRamAddress(uint address)
    {
        address &= 0x1_FFFF;
        if (address >= 0x18000)
        {
            address = (uint)(address & ~0x8000);
        }
        return address;
    }
    #endregion
}
