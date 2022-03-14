using GameboyAdvanced.Core.Ppu.Registers;

namespace GameboyAdvanced.Core.Ppu;

/// <summary>
/// The renderer is responsible for producing a framebuffer once per frame.
/// 
/// This particular renderer does that by rendering a single scanline at a
/// time and is called at the start of each visible hblank.
/// </summary>
internal partial class Ppu
{
    /// <summary>
    /// Whilst iterating over the set of sprites we populate a list of 
    /// properties for each pixel in the scanline so that we can then 
    /// correctly blend sprites with backgrounds.
    /// </summary>
    private struct SpritePixelProperties
    {
        internal int PaletteColor;
        internal SpriteMode PixelMode;
        internal int Priority;
    }

    private struct BgPixelProperties
    {
        internal int PaletteColor;
        internal int Priority;
    }

    private readonly int[][] _scanlineBgBuffer = new int[4][];
    private readonly SpritePixelProperties[] _objBuffer = new SpritePixelProperties[Device.WIDTH];
    private readonly BgPixelProperties[] _bgBuffer = new BgPixelProperties[Device.WIDTH];

    internal void DrawCurrentScanline()
    {
        var bgPriorities = new[] { -1, -1, -1, -1 };
        switch (_dispcnt.BgMode)
        {
            case BgMode.Video0:
                for (var ii = 0; ii < 4; ii++)
                {
                    if (_dispcnt.ScreenDisplayBg[ii])
                    {
                        bgPriorities[ii] = _backgrounds[ii].Control.BgPriority;
                        DrawTextModeScanline(_backgrounds[ii], ref _scanlineBgBuffer[ii]);
                    }
                }

                DrawSpritesOnLine(0x0001_0000);
                CombineBackgroundsInScanline();
                CombineBackgroundsAndSprites();
                break;
            case BgMode.Video1:
                // Background 0-1 are text mode, 2-3 are affine
                for (var ii = 0; ii < 2; ii++)
                {
                    if (_dispcnt.ScreenDisplayBg[ii])
                    {
                        DrawTextModeScanline(_backgrounds[ii], ref _scanlineBgBuffer[ii]);
                    }
                }

                DrawSpritesOnLine(0x0001_0000);

                // TODO - Handle affine backgrounds
                CombineBackgroundsInScanline();
                CombineBackgroundsAndSprites();
                break;
            case BgMode.Video2:
                for (var ii = 0; ii < 4; ii++)
                {
                    if (_dispcnt.ScreenDisplayBg[ii])
                    {
                        // TODO - Handle affine backgrounds
                    }

                    // TODO - How to combine the backgrounds?
                }

                DrawSpritesOnLine(0x0001_0000);
                CombineBackgroundsInScanline();
                CombineBackgroundsAndSprites();
                break;
            case BgMode.Video3:
                if (_dispcnt.ScreenDisplayBg[2])
                {
                    DrawBgMode3CurrentScanline();

                    DrawSpritesOnLine(0x0001_0000);

                    // TODO - Add sprites in to bitmap modes 
                }
                break;
            case BgMode.Video4:
                if (_dispcnt.ScreenDisplayBg[2])
                {
                    DrawBgMode4CurrentScanline();

                    DrawSpritesOnLine(0x0001_4000);

                    // TODO - Add sprites in to bitmap modes
                }
                break;
            case BgMode.Video5:
                if (_dispcnt.ScreenDisplayBg[2])
                {
                    DrawBgMode5CurrentScanline();

                    DrawSpritesOnLine(0x0001_4000);

                    // TODO - Add sprites in to bitmap modes
                }
                break;
            case BgMode.Prohibited6:
            case BgMode.Prohibited7:
                DrawProhibitedModeScanline();
                break;
        }
    }

    private void CombineBackgroundsAndSprites()
    {
        var fbPtr = _currentLine * Device.WIDTH * 4;

        for (var x = 0; x < Device.WIDTH; x++)
        {
            var bgEntry = _bgBuffer[x];
            var spriteEntry = _objBuffer[x];

            var paletteEntry = (bgEntry.Priority <= spriteEntry.Priority, bgEntry.PaletteColor, spriteEntry.PaletteColor) switch
            {
                (_, 0, 0) => BackdropColor,
                (_, 0, _) => _paletteEntries[spriteEntry.PaletteColor],
                (_, _, 0) => _paletteEntries[bgEntry.PaletteColor],
                (true, _, _) => _paletteEntries[bgEntry.PaletteColor],
                (false, _, _) => _paletteEntries[spriteEntry.PaletteColor],
            };

            Utils.ColorToRgb(paletteEntry, _frameBuffer.AsSpan(fbPtr));

            fbPtr += 4;
        }
    }

    private void CombineBackgroundsInScanline()
    {
        var sortedBgIxs = new[] { 0, 1, 2, 3 };
        // Step 1 - Use an optimal sorting network to sort the backgrounds in priority order
        // Network is [[0 1][2 3][0 2][1 3][1 2]]
        // This is absolutely necessary optimisation and I won't hear anything against it
        if (_backgrounds[0].Control.BgPriority > _backgrounds[1].Control.BgPriority)
        {
            sortedBgIxs[0] = 1;
            sortedBgIxs[1] = 0;
        }
        if (_backgrounds[2].Control.BgPriority > _backgrounds[3].Control.BgPriority)
        {
            sortedBgIxs[2] = 3;
            sortedBgIxs[3] = 2;
        }
        if (_backgrounds[sortedBgIxs[0]].Control.BgPriority > _backgrounds[sortedBgIxs[2]].Control.BgPriority)
        {
            var tmp = sortedBgIxs[0];
            sortedBgIxs[0] = sortedBgIxs[2];
            sortedBgIxs[2] = tmp;
        }
        if (_backgrounds[sortedBgIxs[1]].Control.BgPriority > _backgrounds[sortedBgIxs[3]].Control.BgPriority)
        {
            var tmp = sortedBgIxs[1];
            sortedBgIxs[1] = sortedBgIxs[3];
            sortedBgIxs[3] = tmp;
        }
        if (_backgrounds[sortedBgIxs[1]].Control.BgPriority > _backgrounds[sortedBgIxs[2]].Control.BgPriority)
        {
            var tmp = sortedBgIxs[1];
            sortedBgIxs[1] = sortedBgIxs[2];
            sortedBgIxs[2] = tmp;
        }

        for (var x = 0; x < Device.WIDTH; x++)
        {
            var filledPixel = false;
            for (var bgIx = 0; bgIx < 4; bgIx++)
            {
                // Check that the BG is both enabled and that the color is not the backdrop
                if (_dispcnt.ScreenDisplayBg[sortedBgIxs[bgIx]] && _scanlineBgBuffer[sortedBgIxs[bgIx]][x] != 0)
                {
                    _bgBuffer[x].PaletteColor = _scanlineBgBuffer[sortedBgIxs[bgIx]][x];
                    _bgBuffer[x].Priority = _backgrounds[bgIx].Control.BgPriority;
                    filledPixel = true;
                    break; // This was the highest priority pixel
                }
            }

            if (!filledPixel)
            {
                _bgBuffer[x].PaletteColor = 0;
                _bgBuffer[x].Priority = 4;
            }
        }
    }

    private void DrawSpritesOnLine(uint baseTileAddress)
    {
        // Check if OBJs are disabled globally on the PPU
        if (!_dispcnt.ScreenDisplayObj) return;

        // Clear the previous scanlines obj buffer
        for (var ii = 0; ii < Device.WIDTH; ii++)
        {
            _objBuffer[ii].PaletteColor = 0;
            _objBuffer[ii].Priority = 4;
            _objBuffer[ii].PixelMode = SpriteMode.Normal;
        }

        foreach (var sprite in _sprites)
        {
            // Check if the sprite is just disabled
            if (sprite.ObjDisable && sprite.RotationScalingFlag) continue;

            // Sprites that have gone too far beyond the edge of the screen loop back around
            var loopedX = sprite.X >= Device.WIDTH ? sprite.X - 512 : sprite.X;
            var loopedY = sprite.Y >= Device.HEIGHT ? sprite.Y - 256 : sprite.Y;

            // Check if the sprite falls within the scanline
            if (loopedY > _currentLine) continue;
            if (loopedY + sprite.Height < _currentLine) continue;

            // TODO - Double check this behaviour, do we skip prohibited mode sprites
            if (sprite.ObjMode == SpriteMode.Prohibited) continue;

            // TODO - Implement window mode sprites instead of skipping them
            if (sprite.ObjMode == SpriteMode.ObjWindow) continue;

            for (var ii = 0; ii < sprite.Width; ii++)
            {
                var lineX = loopedX + ii;

                // Skip pixels which fall outside the visible area
                if (lineX is < 0 or >= Device.WIDTH) continue;

                // Skip pixels if a higher priority sprite already occupies that pixel
                if (_objBuffer[lineX].Priority < sprite.PriorityRelativeToBg) continue;

                // Work out which pixel relative to the sized texture we're processing
                var textureX = sprite.HorizontalFlip ? sprite.Width - ii - 1 : ii;
                var textureY = sprite.VerticalFlip ? sprite.Height - (_currentLine - loopedY) - 1 : _currentLine - loopedY;
                
                // Decide which tile corresponds to that texture coordinate,
                // this depends on whether we're in 256 color mode and whether
                // the OAM space is configured for 1D or 2D mapping
                var tileAddressOffset = 32 * (sprite.LargePalette, _dispcnt.OneDimObjCharVramMapping) switch
                {
                    (true, true) => sprite.Tile + (textureY / 8 * (sprite.Width / 4)) + (textureX / 4),
                    (true, false) => (sprite.Tile & 0xFFFF_FFFE) + (textureY * 4) + (textureX / 4),
                    (false, true) => sprite.Tile + ((textureY / 8) * (sprite.Width / 8)) + (textureX / 8),
                    (false, false) => sprite.Tile + (textureY * 4) + (textureX / 8),
                };

                tileAddressOffset += ((textureX % 8) * 32) + ((textureY % 8) * 4);

                var tileData = _vram[baseTileAddress + tileAddressOffset];

                // Color 0 in each BG/OBJ palette is transparent so replace with palette 0 color 0
                // TODO - Not taking into account palette mode in BGxCNT
                var pixelPalNo = sprite.LargePalette switch
                {
                    true => tileData,
                    false => ((textureX & 1) == 1) ? (tileData >> 4) & 0b1111 : tileData & 0b1111,
                };

                _objBuffer[lineX].PaletteColor = pixelPalNo;
                _objBuffer[lineX].Priority = sprite.PriorityRelativeToBg;
                _objBuffer[lineX].PixelMode = sprite.ObjMode;
            }
        }
    }

    private void DrawProhibitedModeScanline()
    {
        var fbPtr = Device.WIDTH * 4 * _currentLine; // 4 bytes per pixel
        for (var x = 0; x < Device.WIDTH; x++)
        {
            Utils.ColorToRgb(BackdropColor, _frameBuffer.AsSpan(fbPtr));
            fbPtr += 4;
        }
    }

    private void DrawTextModeScanline(Background background, ref int[] scanlineBuffer)
    {
        // TODO - Mode 0 only implemented in so far as required for Deadbody cpu tests
        // 32*32 tiles, 4 bit color depth
        var charMapBase = background.Control.CharBaseBlock * 0x4000;
        var lineScrolled = _currentLine + background.YOffset;
        var gridY = (lineScrolled / 8) % 32;
        var tileMapBase = (background.Control.ScreenBaseBlock * 0x800) + (gridY * 64);

        // TODO - This is horribly inefficient, going through all 240 pixels one at a time
        //        Could at least do things two at a time since the tile data is the same!
        for (var x = 0; x < Device.WIDTH; x++)
        {
            var scrolledX = x + background.XOffset;
            var gridX = (scrolledX / 8) % 32;

            var tileMapAddress = tileMapBase + (gridX * 2); // 2 bytes per tile map entry
            var tileMap = _vram[tileMapAddress] | (_vram[tileMapAddress + 1] << 8);
            var tile = tileMap & 0b11_1111_1111;
            var horizontalFlip = ((tileMap >> 10) & 1) == 1;
            var verticalFlip = ((tileMap >> 11) & 1) == 1;
            var paletteNumber = ((tileMap >> 12) & 0b1111) << 4;

            var tileX = horizontalFlip ? 7 - (scrolledX % 8) : scrolledX % 8;
            var tileY = verticalFlip ? 7 - (lineScrolled % 8) : lineScrolled % 8;
            var tileAddressOffset = (tileX / 2) + (tileY * 4);
            var tileAddress = charMapBase + (tile * 32) + tileAddressOffset; // 32 bytes per tile
            var tileData = _vram[tileAddress];
            var pixelPalIx = (tileData >> (4 * (tileX & 1))) & 0b1111;
            // Color 0 in each BG/OBJ palette is transparent so replace with palette 0 color 0
            // TODO - Not taking into account palette mode in BGxCNT
            var pixelPalNo = (pixelPalIx == 0) ? 0 : (paletteNumber | pixelPalIx);
            
            if (_currentLine == 60 && x == 80)
            {
                var a = 1;
            }

            scanlineBuffer[x] = pixelPalNo;
        }
    }

    // TODO - Handle affine transformations in bitmap BG modes

    private void DrawBgMode3CurrentScanline()
    {
        var vramPtrBase = _currentLine * 480;
        for (var col = 0; col < 480; col += 2)
        {
            var vramPtr = vramPtrBase + col;
            var fbPtr = 2 * vramPtr;
            var hw = _vram[vramPtr] | (_vram[vramPtr + 1] << 8);
            Utils.ColorToRgb(hw, _frameBuffer.AsSpan(fbPtr));
        }
    }

    private void DrawBgMode4CurrentScanline()
    {
        // BG Mode 4/5 have a double buffer that is switched using Frame1Select on DISPCNT
        var baseAddress = _dispcnt.Frame1Select ? 0x0000_A000 : 0x0;
        var vramPtrBase = _currentLine * 240;
        for (var col = 0; col < 240; col++)
        {
            var vramPtr = vramPtrBase + col;
            var fbPtr = 4 * vramPtr;
            var paletteIndex = _vram[baseAddress + vramPtr];
            var color = _paletteEntries[paletteIndex];
            Utils.ColorToRgb(color, _frameBuffer.AsSpan(fbPtr));
        }
    }

    private void DrawBgMode5CurrentScanline()
    {
        var baseAddress = _dispcnt.Frame1Select ? 0x0000_A000 : 0x0;

        var vramPtrBase = baseAddress + (_currentLine * 320);
        for (var col = 0; col < 480; col += 2)
        {
            var vramPtr = vramPtrBase + col;
            var fbPtr = 2 * vramPtr;
            var hw = _vram[vramPtr] | (_vram[vramPtr + 1] << 8);
            Utils.ColorToRgb(hw, _frameBuffer.AsSpan(fbPtr));
        }
    }
}
