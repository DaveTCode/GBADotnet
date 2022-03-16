﻿using GameboyAdvanced.Core.Ppu.Registers;

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
        internal bool ColorIsPaletteIndex;
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

                CombineBackgroundsInScanline();

                DrawSpritesOnLine(false);
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
                CombineBackgroundsInScanline();

                DrawSpritesOnLine(false);
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

                CombineBackgroundsInScanline();

                DrawSpritesOnLine(false);
                break;
            case BgMode.Video3:
                if (_dispcnt.ScreenDisplayBg[2])
                {
                    DrawBgMode3CurrentScanline();
                }

                DrawSpritesOnLine(true);
                break;
            case BgMode.Video4:
                if (_dispcnt.ScreenDisplayBg[2])
                {
                    DrawBgMode4CurrentScanline();
                }

                DrawSpritesOnLine(true);
                break;
            case BgMode.Video5:
                if (_dispcnt.ScreenDisplayBg[2])
                {
                    DrawBgMode5CurrentScanline();
                }

                DrawSpritesOnLine(true);
                break;
            case BgMode.Prohibited6:
            case BgMode.Prohibited7:
                DrawProhibitedModeScanline();
                break;
        }

        // Final step is to pick the highest priority pixel of each (bg vs obj)
        // TODO - eventually I guess this will do alpha blending etc
        CombineBackgroundsAndSprites();
    }

    private void CombineBackgroundsAndSprites()
    {
        var fbPtr = _currentLine * Device.WIDTH * 4;

        for (var x = 0; x < Device.WIDTH; x++)
        {
            var bgEntry = _bgBuffer[x];
            var spriteEntry = _objBuffer[x];

            if (bgEntry.ColorIsPaletteIndex)
            {
                var paletteEntry = (bgEntry.PaletteColor, spriteEntry.PaletteColor & 0b1111, bgEntry.Priority >= spriteEntry.Priority) switch 
                {
                    (0, 0, _) => BackdropColor,
                    (0, _, _) => _paletteEntries[0x100 + spriteEntry.PaletteColor],
                    (_, 0, _) => _paletteEntries[bgEntry.PaletteColor],
                    (_, _, true) => _paletteEntries[0x100 + spriteEntry.PaletteColor],
                    (_, _, false) => _paletteEntries[bgEntry.PaletteColor],
                };

                Utils.ColorToRgb(paletteEntry, _frameBuffer.AsSpan(fbPtr));
            }
            else
            {
                if (spriteEntry.Priority <= bgEntry.Priority && (spriteEntry.PaletteColor & 0b1111) != 0)
                {
                    Utils.ColorToRgb(_paletteEntries[0x100 + spriteEntry.PaletteColor], _frameBuffer.AsSpan(fbPtr));
                }
                else
                {
                    Utils.ColorToRgb(bgEntry.PaletteColor, _frameBuffer.AsSpan(fbPtr));
                }
            }
            
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
                    _bgBuffer[x].ColorIsPaletteIndex = true;
                    filledPixel = true;
                    break; // This was the highest priority pixel
                }
            }

            if (!filledPixel)
            {
                _bgBuffer[x].PaletteColor = 0;
                _bgBuffer[x].Priority = 4;
                _bgBuffer[x].ColorIsPaletteIndex = true;
            }
        }
    }

    private void DrawSpritesOnLine(bool bitmapMode)
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
            if (loopedY + sprite.Height <= _currentLine) continue;

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
                if (_objBuffer[lineX].Priority <= sprite.PriorityRelativeToBg && _objBuffer[lineX].PaletteColor != 0) continue;

                // Work out which pixel relative to the sprite texture we're processing
                var spriteX = sprite.HorizontalFlip ? sprite.Width - ii - 1 : ii;
                var spriteY = sprite.VerticalFlip ? sprite.Height - (_currentLine - loopedY) : _currentLine - loopedY;

                // The sprite is made up of N(>=1) 8*8 tiles in each direction,
                // work out which of these tiles is in use
                var spriteGridX = spriteX / 8;
                var spriteGridY = spriteY / 8;

                // Decide which tile corresponds to that texture coordinate,
                // this depends on whether we're in 256 color mode and whether
                // the OAM space is configured for 1D or 2D mapping
                var finalTileNumber = (sprite.LargePalette, _dispcnt.OneDimObjCharVramMapping) switch
                {
                    (true, true) => sprite.Tile + (spriteGridY * sprite.Width) + spriteGridX,
                    (true, false) => (sprite.Tile & 0xFFFF_FFFE) + (spriteGridY * 4) + spriteGridX, // TODO - Not sure
                    (false, true) => sprite.Tile + (spriteGridY * sprite.Width / 8) + spriteGridX,
                    (false, false) => sprite.Tile + (spriteGridY * 32) + spriteGridX,
                };
                finalTileNumber &= 0x3FF; // Wrap around tiles

                if (bitmapMode && finalTileNumber < 0x200) continue;

                var finalTileAddressOffset = finalTileNumber * (sprite.LargePalette ? 64 : 32);

                var pixelByteAddress = 0x0001_0000 + finalTileAddressOffset + ((spriteX % 8) / 2) + ((spriteY % 8) * 4);

                var tileData = _vram[pixelByteAddress];

                // Color 0 in each BG/OBJ palette is transparent so replace with palette 0 color 0
                // TODO - Not taking into account palette mode in BGxCNT
                var pixelPalNo = sprite.LargePalette switch
                {
                    true => tileData,
                    false => (sprite.PaletteNumber << 4) | (tileData >> (4 * (spriteX & 1))) & 0b1111,
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
        var tileMapBase = background.Control.ScreenBaseBlock * 0x800;

        for (var x = 0; x < Device.WIDTH; x++)
        {
            // Apply the scroll to the x,y screen coordinates to get tilemap coordinates
            var scrolledY = (_currentLine + background.YOffset) % background.Control.ScreenSize.Height();
            var scrolledX = (x + background.XOffset) % background.Control.ScreenSize.Width();

            // Convert the x,y coordinates into which tile coordinates
            var screenBlockTileX = scrolledX / 8;
            var screenBlockTileY = scrolledY / 8;

            // If the background is > 32*32 then we need to work out which
            // screen block is in use so we can apply an offset to the
            // tilemap address
            var screenBlockOffset = background.Control.ScreenSize switch
            {
                BgSize.Regular32x32 => 0,
                BgSize.Regular32x64 => (screenBlockTileY / 32) * 2,
                BgSize.Regular64x32 => (screenBlockTileX / 32),
                BgSize.Regular64x64 => (screenBlockTileX / 32) + ((screenBlockTileY / 32) * 2),
            };

            var tileMapAddress = tileMapBase + (screenBlockOffset * 0x800) + ((screenBlockTileY % 32) * 64) + (2 * (screenBlockTileX % 32));
            var tileMapEntry = _vram[tileMapAddress] | (_vram[tileMapAddress + 1] << 8);
            var tile = tileMapEntry & 0b11_1111_1111;
            var horizontalFlip = ((tileMapEntry >> 10) & 1) == 1;
            var verticalFlip = ((tileMapEntry >> 11) & 1) == 1;
            var paletteNumber = ((tileMapEntry >> 12) & 0b1111) << 4;

            var tileX = horizontalFlip ? 7 - (scrolledX % 8) : scrolledX % 8;
            var tileY = verticalFlip ? 7 - (scrolledY % 8) : scrolledY % 8;

            if (background.Control.LargePalette)
            {
                var tileAddress = (background.Control.CharBaseBlock * 0x4000) + (tile * 64) + tileX + (tileY * 8);
                scanlineBuffer[x] = _vram[tileAddress];
            }
            else
            {
                var tileAddress = (background.Control.CharBaseBlock * 0x4000) + (tile * 32) + (tileX / 2) + (tileY * 4);
                var pixelPalIx = (_vram[tileAddress] >> (4 * (tileX & 1))) & 0b1111;
                var pixelPalNo = (pixelPalIx == 0) ? 0 : (paletteNumber | pixelPalIx);
                scanlineBuffer[x] = pixelPalNo;
            }
        }
    }

    // TODO - Handle affine transformations in bitmap BG modes

    private void DrawBgMode3CurrentScanline()
    {
        var vramPtrBase = _currentLine * 480;
        for (var col = 0; col < 240; col ++)
        {
            var vramPtr = vramPtrBase + (col * 2);
            var hw = _vram[vramPtr] | (_vram[vramPtr + 1] << 8);
            _bgBuffer[col].PaletteColor = hw;
            _bgBuffer[col].Priority = _backgrounds[2].Control.BgPriority;
            _bgBuffer[col].ColorIsPaletteIndex = false;
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
            var paletteIndex = _vram[baseAddress + vramPtr];
            _bgBuffer[col].PaletteColor = paletteIndex;
            _bgBuffer[col].Priority = _backgrounds[2].Control.BgPriority;
            _bgBuffer[col].ColorIsPaletteIndex = true;
        }
    }

    private void DrawBgMode5CurrentScanline()
    {
        var baseAddress = _dispcnt.Frame1Select ? 0x0000_A000 : 0x0;

        var vramPtrBase = baseAddress + (_currentLine * 320);
        for (var col = 0; col < 240; col++)
        {
            var vramPtr = vramPtrBase + (col * 2);
            var hw = _vram[vramPtr] | (_vram[vramPtr + 1] << 8);
            _bgBuffer[col].PaletteColor = hw;
            _bgBuffer[col].Priority = _backgrounds[2].Control.BgPriority;
            _bgBuffer[col].ColorIsPaletteIndex = false;
        }
    }
}
