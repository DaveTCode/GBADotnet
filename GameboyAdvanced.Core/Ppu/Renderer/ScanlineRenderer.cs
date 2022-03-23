using GameboyAdvanced.Core.Ppu.Registers;

namespace GameboyAdvanced.Core.Ppu;

/// <summary>
/// The renderer is responsible for producing a framebuffer once per frame.
/// 
/// This particular renderer does that by rendering a single scanline at a
/// time and is called at the start of each visible hblank.
/// </summary>
public partial class Ppu
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
        internal int[] PaletteColor;
        internal int[] Priority;
        internal bool[] ColorIsPaletteIndex;
        internal int[] BgId;

        public BgPixelProperties()
        {
            PaletteColor = new int[2];
            Priority = new int[2];
            ColorIsPaletteIndex = new bool[2];
            BgId = new int[2];
        }
    }

    private readonly int[][] _scanlineBgBuffer = new int[4][];
    private readonly SpritePixelProperties[] _objBuffer = new SpritePixelProperties[Device.WIDTH];
    private readonly BgPixelProperties[] _bgBuffer = new BgPixelProperties[Device.WIDTH];
    private readonly byte[][] _pixels = new byte[2][]
    {
        new byte[4], new byte[4]
    };


    internal void DrawCurrentScanline()
    {
        var sortedBgIxs = new[] { 0, 1, 2, 3 };
        SortBackgroundPriorities(sortedBgIxs);

        switch (Dispcnt.BgMode)
        {
            case BgMode.Video0:
                for (var ii = 0; ii < 4; ii++)
                {
                    if (Dispcnt.ScreenDisplayBg[ii])
                    {
                        DrawTextModeScanline(Backgrounds[ii], ref _scanlineBgBuffer[ii]);
                    }
                }
                break;
            case BgMode.Video1:
                // Background 0-1 are text mode, 2-3 are affine
                for (var ii = 0; ii < 4; ii++)
                {
                    if (Dispcnt.ScreenDisplayBg[ii])
                    {
                        if (ii < 2)
                        {
                            DrawTextModeScanline(Backgrounds[ii], ref _scanlineBgBuffer[ii]);
                        }
                        else
                        {
                            DrawAffineModeScanline(Backgrounds[ii], ref _scanlineBgBuffer[ii]);
                        }
                    }
                }
                break;
            case BgMode.Video2:
                for (var ii = 0; ii < 4; ii++)
                {
                    if (Dispcnt.ScreenDisplayBg[ii])
                    {
                        DrawAffineModeScanline(Backgrounds[ii], ref _scanlineBgBuffer[ii]);
                    }
                }
                break;
            case BgMode.Video3:
                if (Dispcnt.ScreenDisplayBg[2])
                {
                    DrawBgMode3CurrentScanline(ref _scanlineBgBuffer[2]);
                }
                break;
            case BgMode.Video4:
                if (Dispcnt.ScreenDisplayBg[2])
                {
                    DrawBgMode4CurrentScanline(ref _scanlineBgBuffer[2]);
                }
                break;
            case BgMode.Video5:
                if (Dispcnt.ScreenDisplayBg[2])
                {
                    DrawBgMode5CurrentScanline(ref _scanlineBgBuffer[2]);
                }
                break;
            case BgMode.Prohibited6:
            case BgMode.Prohibited7:
                DrawProhibitedModeScanline();
                break;
        }

        // Final step is to pick the highest priority pixel of each (bg vs obj)
        // TODO - eventually I guess this will do alpha blending etc
        CombineBackgroundsAndSprites(sortedBgIxs);
    }

    private void CombineBackgroundsAndSprites(int[] sortedBgIxs)
    {
        var fbPtr = CurrentLine * Device.WIDTH * 4;
        var usingPaletteIndex = Dispcnt.BgMode is not (BgMode.Video3 or BgMode.Video5);

        for (var x = 0; x < Device.WIDTH; x++)
        {
            // Set the fallback values for the pixel
            _bgBuffer[x].PaletteColor[0] = _bgBuffer[x].PaletteColor[1] = 0;
            _bgBuffer[x].Priority[0] = _bgBuffer[x].Priority[1] = 4;
            _bgBuffer[x].ColorIsPaletteIndex[0] = _bgBuffer[x].ColorIsPaletteIndex[1] = usingPaletteIndex;
            _bgBuffer[x].BgId[0] = _bgBuffer[x].BgId[1] = 0;

            var usingWindows = Dispcnt.WindowDisplay[0] | Dispcnt.WindowDisplay[1] | Dispcnt.ObjWindowDisplay;

            // First step is to work out which window (if any) this pixel is
            // part of so we know which backgrounds/objects might be present
            var activeWindow = _windows.GetHighestPriorityWindow(Dispcnt, x, CurrentLine);

            // Then fill in the bgBuffer with the top two highest priority
            // opaque pixels
            var filledBgPixels = 0;
            for (var unsortedBgIx = 0; unsortedBgIx < 4; unsortedBgIx++)
            {
                var sortedBgIx = sortedBgIxs[unsortedBgIx];

                // Check that the BG is both enabled and that the color is not the backdrop
                if (Dispcnt.ScreenDisplayBg[sortedBgIx] && _scanlineBgBuffer[sortedBgIx][x] != 0)
                {
                    // Now check that the window this background is in (if any) has that background enabled
                    if (!usingWindows ||
                        (activeWindow == -1 && _windows.BgEnableOutside[sortedBgIx]) ||
                        (activeWindow != -1 && _windows.BgEnableInside[activeWindow][sortedBgIx]))
                    {
                        _bgBuffer[x].PaletteColor[filledBgPixels] = _scanlineBgBuffer[sortedBgIx][x];
                        _bgBuffer[x].Priority[filledBgPixels] = Backgrounds[sortedBgIx].Control.BgPriority;
                        _bgBuffer[x].ColorIsPaletteIndex[filledBgPixels] = usingPaletteIndex;
                        _bgBuffer[x].BgId[filledBgPixels] = sortedBgIx;
                        filledBgPixels++;

                        if (filledBgPixels == 1 && ColorEffects.SpecialEffect is SpecialEffect.None or SpecialEffect.IncreaseBrightness or SpecialEffect.DecreaseBrightness)
                        {
                            // No second blend target and found highest priority pixel so break
                            break;
                        }
                        else if (filledBgPixels == 2)
                        {
                            break;
                        }
                    }
                }
            }

            var bgEntry = _bgBuffer[x];
            var spriteEntry = _objBuffer[x];
            var spriteWindowEnabled = !usingWindows || (activeWindow == -1 && _windows.ObjEnableOutside) || (activeWindow != -1 && _windows.ObjEnableInside[activeWindow]);

            // Work out what are the top target palette entries and determine
            // if those targets were enabled for color effects
            var colorEffectsValid = ColorEffects.SpecialEffect != SpecialEffect.None;
            var objLayerUsed = false;
            var bgLayerIx = 0;
            for (var target = 0; target < (ColorEffects.SpecialEffect == SpecialEffect.AlphaBlend ? 2 : 1); target++)
            {
                if (bgEntry.ColorIsPaletteIndex[bgLayerIx])
                {
                    PaletteEntry paletteEntry;
                    if (bgEntry.PaletteColor[bgLayerIx] == 0 && ((spriteEntry.PaletteColor & 0b1111) == 0))
                    {
                        paletteEntry = BackdropColor;
                        colorEffectsValid &= ColorEffects.TargetBackdrop[target];
                        bgLayerIx++;
                    }
                    else if (bgEntry.PaletteColor[bgLayerIx] == 0 && !objLayerUsed)
                    {
                        paletteEntry = _paletteEntries[0x100 + spriteEntry.PaletteColor];
                        colorEffectsValid &= ColorEffects.TargetObj[target];
                        objLayerUsed = true;
                    }
                    else if ((spriteEntry.PaletteColor & 0b1111) == 0)
                    {
                        paletteEntry = _paletteEntries[bgEntry.PaletteColor[bgLayerIx]];
                        colorEffectsValid &= ColorEffects.TargetBg[target][bgEntry.BgId[bgLayerIx]];
                        bgLayerIx++;
                    }
                    else if (bgEntry.Priority[target] >= spriteEntry.Priority && spriteWindowEnabled && !objLayerUsed)
                    {
                        paletteEntry = _paletteEntries[0x100 + spriteEntry.PaletteColor];
                        colorEffectsValid &= ColorEffects.TargetObj[target];
                        objLayerUsed = true;
                    }
                    else
                    {
                        paletteEntry = _paletteEntries[bgEntry.PaletteColor[bgLayerIx]];
                        colorEffectsValid &= ColorEffects.TargetBg[target][bgEntry.BgId[bgLayerIx]];
                        bgLayerIx++;
                    }

                    Utils.ColorToRgb(paletteEntry, _pixels[target]);
                }
                else
                {
                    if (spriteEntry.Priority <= bgEntry.Priority[bgLayerIx] && (spriteEntry.PaletteColor & 0b1111) != 0 && spriteWindowEnabled && !objLayerUsed)
                    {
                        colorEffectsValid &= ColorEffects.TargetObj[target];
                        Utils.ColorToRgb(_paletteEntries[0x100 + spriteEntry.PaletteColor], _pixels[target]);
                        objLayerUsed = true;
                    }
                    else
                    {
                        if (bgEntry.PaletteColor[bgLayerIx] == 0)
                        {
                            colorEffectsValid &= ColorEffects.TargetBackdrop[target];
                        }
                        else
                        {
                            colorEffectsValid &= ColorEffects.TargetBg[target][bgEntry.BgId[bgLayerIx]];
                        }
                        
                        Utils.ColorToRgb(bgEntry.PaletteColor[bgLayerIx], _pixels[target]);
                        bgLayerIx++;
                    }
                }
            }

            if (!colorEffectsValid || ColorEffects.SpecialEffect == SpecialEffect.None)
            {
                // Just copy the highest priority pixel into the frame buffer as no color effects to apply
                Array.Copy(_pixels[0], 0, FrameBuffer, fbPtr, 4);
            }
            else
            {
                switch (ColorEffects.SpecialEffect)
                {
                    case SpecialEffect.AlphaBlend:
                        for (int i = 0; i < 4; i++)
                        {
                            FrameBuffer[fbPtr + i] = ColorEffects.AlphaIntensity(_pixels[0][i], _pixels[1][i]);
                        }
                        break;
                    case SpecialEffect.IncreaseBrightness:
                        for (int i = 0; i < 4; i++)
                        {
                            FrameBuffer[fbPtr + i] = ColorEffects.BrightnessIncrease(_pixels[0][i]);
                        }
                        break;
                    case SpecialEffect.DecreaseBrightness:
                        for (int i = 0; i < 4; i++)
                        {
                            FrameBuffer[fbPtr + i] = ColorEffects.BrightnessDecrease(_pixels[0][i]);
                        }
                        break;
                }
            }

            fbPtr += 4;
        }
    }

    private void SortBackgroundPriorities(int[] sortedBgIxs)
    {
        // Use an optimal sorting network to sort the backgrounds in priority order
        // Network is [[0 1][2 3][0 2][1 3][1 2]]
        // This is absolutely necessary optimisation and I won't hear anything against it
        if (Backgrounds[0].Control.BgPriority > Backgrounds[1].Control.BgPriority)
        {
            sortedBgIxs[0] = 1;
            sortedBgIxs[1] = 0;
        }
        if (Backgrounds[2].Control.BgPriority > Backgrounds[3].Control.BgPriority)
        {
            sortedBgIxs[2] = 3;
            sortedBgIxs[3] = 2;
        }
        if (Backgrounds[sortedBgIxs[0]].Control.BgPriority > Backgrounds[sortedBgIxs[2]].Control.BgPriority)
        {
            var tmp = sortedBgIxs[0];
            sortedBgIxs[0] = sortedBgIxs[2];
            sortedBgIxs[2] = tmp;
        }
        if (Backgrounds[sortedBgIxs[1]].Control.BgPriority > Backgrounds[sortedBgIxs[3]].Control.BgPriority)
        {
            var tmp = sortedBgIxs[1];
            sortedBgIxs[1] = sortedBgIxs[3];
            sortedBgIxs[3] = tmp;
        }
        if (Backgrounds[sortedBgIxs[1]].Control.BgPriority > Backgrounds[sortedBgIxs[2]].Control.BgPriority)
        {
            var tmp = sortedBgIxs[1];
            sortedBgIxs[1] = sortedBgIxs[2];
            sortedBgIxs[2] = tmp;
        }
    }

    private void DrawSpritesOnLine(bool bitmapMode)
    {
        // Clear the previous scanlines obj buffer
        for (var ii = 0; ii < Device.WIDTH; ii++)
        {
            _objBuffer[ii].PaletteColor = 0;
            _objBuffer[ii].Priority = 4;
            _objBuffer[ii].PixelMode = SpriteMode.Normal;
        }

        // Check if OBJs are disabled globally on the PPU
        if (!Dispcnt.ScreenDisplayObj) return;

        foreach (var sprite in _sprites)
        {
            // Check if the sprite is just disabled (affine sprites can't be
            // disabled but we force the value to false so no check required
            // here)
            if (sprite.ObjDisable) continue;

            // Sprites that have gone too far beyond the edge of the screen loop back around
            var loopedX = sprite.X >= Device.WIDTH ? sprite.X - 512 : sprite.X;
            var loopedY = sprite.Y >= Device.HEIGHT ? sprite.Y - 256 : sprite.Y;

            // TODO - Double check this behaviour, do we skip prohibited mode sprites
            if (sprite.ObjMode == SpriteMode.Prohibited) continue;

            // TODO - Implement window mode sprites instead of skipping them
            if (sprite.ObjMode == SpriteMode.ObjWindow) continue;

            // Affine double size sprites are contained with a double size bounding box
            var spriteWidth = (sprite.IsAffine && sprite.DoubleSize) ? sprite.Width * 2 : sprite.Width;
            var spriteHeight = (sprite.IsAffine && sprite.DoubleSize) ? sprite.Height * 2 : sprite.Height;

            // Check if the sprite falls within the scanline (counting the bounding box)
            if (loopedY > CurrentLine) continue;
            if (loopedY + spriteHeight <= CurrentLine) continue;

            for (var ii = 0; ii < spriteWidth; ii++)
            {
                var lineX = loopedX + ii;

                // Skip pixels which fall outside the visible area
                if (lineX is < 0 or >= Device.WIDTH) continue;

                // Skip pixels if a higher priority sprite already occupies that pixel
                if (_objBuffer[lineX].Priority <= sprite.PriorityRelativeToBg && (_objBuffer[lineX].PaletteColor & 0b1111) != 0) continue;

                // Work out which pixel relative to the sprite texture we're processing
                var spriteX = sprite.HorizontalFlip && !sprite.IsAffine ? sprite.Width - ii - 1 : ii;
                var spriteY = sprite.VerticalFlip && !sprite.IsAffine ? sprite.Height - (CurrentLine - loopedY) : CurrentLine - loopedY;

                if (sprite.IsAffine)
                {
                    spriteX -= (spriteWidth / 2);
                    spriteY -= (spriteHeight / 2);
                    _spriteAffineTransforms[sprite.AffineGroup].TransformVector(ref spriteX, ref spriteY);
                    spriteX += (sprite.Width / 2);
                    spriteY += (sprite.Height / 2);

                    if (spriteX >= sprite.Width || spriteX < 0 || spriteY >= sprite.Height || spriteY < 0) continue;
                }

                // The sprite is made up of N(>=1) 8*8 tiles in each direction,
                // work out which of these tiles is in use
                var spriteGridX = spriteX / 8;
                var spriteGridY = spriteY / 8;

                // Decide which tile corresponds to that texture coordinate,
                // this depends on whether we're in 256 color mode and whether
                // the OAM space is configured for 1D or 2D mapping
                var finalTileNumber = (sprite.LargePalette, Dispcnt.OneDimObjCharVramMapping) switch
                {
                    (true, true) => sprite.Tile + (spriteGridY * sprite.Width / 4) + (spriteGridX * 2),
                    (true, false) => (sprite.Tile & 0xFFFF_FFFE) + (spriteGridY * 32) + (spriteGridX * 2),
                    (false, true) => sprite.Tile + (spriteGridY * sprite.Width / 8) + spriteGridX,
                    (false, false) => sprite.Tile + (spriteGridY * 32) + spriteGridX,
                };
                finalTileNumber &= 0x3FF; // Wrap around tiles

                if (bitmapMode && finalTileNumber < 0x200) continue;

                var finalTileAddressOffset = finalTileNumber * 32;

                var pixelByteAddress = 0x0001_0000
                    + finalTileAddressOffset
                    + ((spriteX % 8) / (sprite.LargePalette ? 1 : 2))
                    + ((spriteY % 8) * (sprite.LargePalette ? 8 : 4));

                var tileData = Vram[pixelByteAddress];

                // Color 0 in each BG/OBJ palette is transparent so replace with palette 0 color 0
                var pixelPalNo = sprite.LargePalette switch
                {
                    true => tileData,
                    false => (sprite.PaletteNumber << 4) | ((tileData >> (4 * (spriteX & 1))) & 0b1111),
                };

                _objBuffer[lineX].PaletteColor = pixelPalNo;
                _objBuffer[lineX].Priority = sprite.PriorityRelativeToBg;
                _objBuffer[lineX].PixelMode = sprite.ObjMode;
            }
        }
    }

    private void DrawProhibitedModeScanline()
    {
        var fbPtr = Device.WIDTH * 4 * CurrentLine; // 4 bytes per pixel
        for (var x = 0; x < Device.WIDTH; x++)
        {
            Utils.ColorToRgb(BackdropColor, FrameBuffer.AsSpan(fbPtr));
            fbPtr += 4;
        }
    }

    private void DrawAffineModeScanline(Background background, ref int[] scanlineBuffer)
    {
        var tileMapBaseAddress = background.Control.ScreenBaseBlock * 0x800;
        var tileBaseAddress = background.Control.CharBaseBlock * 0x4000;
        var (size, blockWidth) = background.Control.ScreenSize switch
        {
            BgSize.Regular32x32 => (128, 16),
            BgSize.Regular64x32 => (256, 32),
            BgSize.Regular32x64 => (512, 64),
            BgSize.Regular64x64 => (1024, 128),
            _ => throw new Exception("Invalid bg size")
        };
        var baseRefX = background.RefPointX + (background.Dmx * CurrentLine);
        var baseRefY = background.RefPointY + (background.Dmy * CurrentLine);

        for (var pixel = 0; pixel < Device.WIDTH; pixel++)
        {
            var xBase = baseRefX >> 8;
            var yBase = baseRefY >> 8;
            baseRefX += background.Dx;
            baseRefY += background.Dy;

            // Affine backgrounds can either make things outside the area transparent or wrap for overflow
            if (background.Control.DisplayAreaOverflow)
            {
                if (xBase >= size) xBase %= size;
                else if (xBase < 0) xBase = size + (xBase % size);

                if (yBase >= size) yBase %= size;
                else if (yBase < 0) yBase = size + (yBase % size);
            }
            else if (xBase < 0 || xBase >= size || yBase < 0 || yBase >= size)
            {
                scanlineBuffer[pixel] = 0;
                continue;
            }

            var tile = Vram[tileMapBaseAddress + ((yBase / 8) * blockWidth) + (xBase / 8)];
            var tileAddress = tileBaseAddress + (tile * 64); // All affine backgrounds use 8bpp tiles
            tileAddress += ((yBase % 8) * 8) + (xBase % 8);
            var paletteIndex = Vram[tileAddress];
            scanlineBuffer[pixel] = paletteIndex;
        }
    }

    private void DrawTextModeScanline(Background background, ref int[] scanlineBuffer)
    {
        var tileMapBase = background.Control.ScreenBaseBlock * 0x800;

        for (var x = 0; x < Device.WIDTH; x++)
        {
            // Apply the scroll to the x,y screen coordinates to get tilemap coordinates
            var scrolledY = (CurrentLine + background.YOffset) % background.Control.ScreenSize.Height();
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
                _ => throw new Exception("Invalid bg size")
            };

            var tileMapAddress = tileMapBase + (screenBlockOffset * 0x800) + ((screenBlockTileY % 32) * 64) + (2 * (screenBlockTileX % 32));
            var tileMapEntry = Vram[tileMapAddress] | (Vram[tileMapAddress + 1] << 8);
            var tile = tileMapEntry & 0b11_1111_1111;
            var horizontalFlip = ((tileMapEntry >> 10) & 1) == 1;
            var verticalFlip = ((tileMapEntry >> 11) & 1) == 1;
            var paletteNumber = ((tileMapEntry >> 12) & 0b1111) << 4;

            var tileX = horizontalFlip ? 7 - (scrolledX % 8) : scrolledX % 8;
            var tileY = verticalFlip ? 7 - (scrolledY % 8) : scrolledY % 8;

            if (background.Control.LargePalette)
            {
                var tileAddress = (background.Control.CharBaseBlock * 0x4000) + (tile * 64) + tileX + (tileY * 8);
                scanlineBuffer[x] = Vram[tileAddress];
            }
            else
            {
                var tileAddress = (background.Control.CharBaseBlock * 0x4000) + (tile * 32) + (tileX / 2) + (tileY * 4);
                var pixelPalIx = (Vram[tileAddress] >> (4 * (tileX & 1))) & 0b1111;
                var pixelPalNo = (pixelPalIx == 0) ? 0 : (paletteNumber | pixelPalIx);
                scanlineBuffer[x] = pixelPalNo;
            }
        }
    }

    // TODO - Handle affine transformations in bitmap BG modes

    private void DrawBgMode3CurrentScanline(ref int[] scanlineBgBuffer)
    {
        var vramPtrBase = CurrentLine * 480;
        for (var col = 0; col < 240; col++)
        {
            var vramPtr = vramPtrBase + (col * 2);
            var hw = Vram[vramPtr] | (Vram[vramPtr + 1] << 8);
            scanlineBgBuffer[col] = hw;
        }
    }

    private void DrawBgMode4CurrentScanline(ref int[] scanlineBgBuffer)
    {
        // BG Mode 4/5 have a double buffer that is switched using Frame1Select on DISPCNT
        var baseAddress = Dispcnt.Frame1Select ? 0x0000_A000 : 0x0;
        var vramPtrBase = CurrentLine * 240;
        for (var col = 0; col < 240; col++)
        {
            var vramPtr = vramPtrBase + col;
            var paletteIndex = Vram[baseAddress + vramPtr];
            scanlineBgBuffer[col] = paletteIndex;
        }
    }

    private void DrawBgMode5CurrentScanline(ref int[] scanlineBgBuffer)
    {
        // Screen is 160*128 in this mode, the rest gets backdrop color
        var baseAddress = Dispcnt.Frame1Select ? 0x0000_A000 : 0x0;

        var vramPtrBase = baseAddress + (CurrentLine * 320);
        for (var col = 0; col < 240; col++)
        {
            var vramPtr = vramPtrBase + (col * 2);
            var hw = Vram[vramPtr] | (Vram[vramPtr + 1] << 8);

            if (col >= 160 || CurrentLine >= 128)
            {
                scanlineBgBuffer[col] = 0;
            }
            else
            {
                scanlineBgBuffer[col] = hw;
            }
        }
    }
}
