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

                CombineBackgroundsScanline();
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

                // TODO - Handle affine backgrounds

                CombineBackgroundsScanline();
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
                break;
            case BgMode.Video3:
                if (_dispcnt.ScreenDisplayBg[2])
                {
                    DrawBgMode3CurrentScanline();
                }
                break;
            case BgMode.Video4:
                if (_dispcnt.ScreenDisplayBg[2])
                {
                    DrawBgMode4CurrentScanline();
                }
                break;
            case BgMode.Video5:
                if (_dispcnt.ScreenDisplayBg[2])
                {
                    DrawBgMode5CurrentScanline();
                }
                break;
            case BgMode.Prohibited6:
            case BgMode.Prohibited7:
                DrawProhibitedModeScanline();
                break;
        }
    }

    private void CombineBackgroundsScanline()
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

        var fbPtr = _currentLine * Device.WIDTH * 4;
        for (var x = 0; x < Device.WIDTH; x++)
        {
            var filledPixel = false;
            for (var bgIx = 0; bgIx < 4; bgIx++)
            {
                // Check that the BG is both enabled and that the color is not the backdrop
                if (_dispcnt.ScreenDisplayBg[sortedBgIxs[bgIx]] && _scanlineBgBuffer[sortedBgIxs[bgIx]][x] != 0)
                {
                    Utils.ColorToRgb(_paletteEntries[_scanlineBgBuffer[sortedBgIxs[bgIx]][x]], _frameBuffer.AsSpan(fbPtr));
                    filledPixel = true;
                    break; // This was the highest priority pixel
                }
            }

            if (!filledPixel)
            {
                Utils.ColorToRgb(BackdropColor, _frameBuffer.AsSpan(fbPtr));
            }

            fbPtr += 4;
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
            var _horizontalFlip = ((tileMap >> 10) & 1) == 1;
            var _verticalFlip = ((tileMap >> 11) & 1) == 1;
            var paletteNumber = ((tileMap >> 12) & 0b1111) << 4;

            var tileX = _horizontalFlip ? 7 - (scrolledX % 8) : scrolledX % 8;
            var tileY = _verticalFlip ? 7 - (lineScrolled % 8) : lineScrolled % 8;
            var tileAddressOffset = (tileX / 2) + (tileY * 4);
            var tileAddress = charMapBase + (tile * 32) + tileAddressOffset; // 32 bytes per tile
            var tileData = _vram[tileAddress];
            var pixelPalIx = (tileData >> (4 * (tileX & 1))) & 0b1111;
            // Color 0 in each BG/OBJ palette is transparent so replace with palette 0 color 0
            // TODO - Not taking into account palette mode in BGxCNT
            var pixelPalNo = (pixelPalIx == 0) ? 0 : (paletteNumber | pixelPalIx);
            
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
