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

        public override string ToString() => $"Prio={Priority}, Mode={PixelMode}, Color={PaletteColor}";
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

        public override string ToString()
        {
            return $"BG{BgId[0]},Prio={Priority[0]}, {PaletteColor[0]:X2} - BG{BgId[1]},Prio={Priority[1]}, {PaletteColor[1]:X2}";
        }
    }

    private int[] _windowState = new int[Device.WIDTH];
    private readonly int[][] _scanlineBgBuffer = new int[4][];
    private readonly SpritePixelProperties[] _objBuffer = new SpritePixelProperties[Device.WIDTH];
    private readonly BgPixelProperties[] _bgBuffer = new BgPixelProperties[Device.WIDTH];
    private readonly byte[][] _pixels = new byte[2][]
    {
        new byte[4], new byte[4]
    };


    internal void DrawCurrentScanline()
    {
        // Forced blanking draws backdrop color across the whole scanline
        if (Dispcnt.ForcedBlank)
        {
            DrawBlankScanline();
            return;
        }

        CalculateScanlineWindowState();

        var sortedBgIxs = new[] { 0, 1, 2, 3 };
        SortBackgroundPriorities(sortedBgIxs);

        switch (Dispcnt.BgMode)
        {
            case BgMode.Video0:
                if (Dispcnt.ScreenDisplayBg[0]) DrawTextModeScanline(Backgrounds[0], ref _scanlineBgBuffer[0]);
                if (Dispcnt.ScreenDisplayBg[1]) DrawTextModeScanline(Backgrounds[1], ref _scanlineBgBuffer[1]);
                if (Dispcnt.ScreenDisplayBg[2]) DrawTextModeScanline(Backgrounds[2], ref _scanlineBgBuffer[2]);
                if (Dispcnt.ScreenDisplayBg[3]) DrawTextModeScanline(Backgrounds[3], ref _scanlineBgBuffer[3]);
                break;
            case BgMode.Video1:
                // Background 0-1 are text mode, 2 is affine
                if (Dispcnt.ScreenDisplayBg[0]) DrawTextModeScanline(Backgrounds[0], ref _scanlineBgBuffer[0]);
                if (Dispcnt.ScreenDisplayBg[1]) DrawTextModeScanline(Backgrounds[1], ref _scanlineBgBuffer[1]);
                if (Dispcnt.ScreenDisplayBg[2]) DrawAffineModeScanline(Backgrounds[2], ref _scanlineBgBuffer[2]);
                break;
            case BgMode.Video2:
                if (Dispcnt.ScreenDisplayBg[2]) DrawAffineModeScanline(Backgrounds[2], ref _scanlineBgBuffer[2]);
                if (Dispcnt.ScreenDisplayBg[3]) DrawAffineModeScanline(Backgrounds[3], ref _scanlineBgBuffer[3]);
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

        CombineBackgroundsAndSprites(sortedBgIxs);
    }

    private void CalculateScanlineWindowState()
    {
        if (Dispcnt.WindowDisplay[0] || Dispcnt.WindowDisplay[1] || Dispcnt.ObjWindowDisplay)
        {
            for (var x = 0; x < Device.WIDTH; x++)
            {
                var windowState = _windows.GetHighestPriorityWindow(Dispcnt, x, CurrentLine);

                // Leave obj windows alone iff the non-obj windows are inactive
                _windowState[x] = windowState != -1 ? windowState : _windowState[x];
            }
        }
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

            // The window state is cached per scanline by both the sprite
            // handling and the normal window handling.
            var activeWindow = _windowState[x];

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
                        (activeWindow > -1 && activeWindow < 2 && _windows.BgEnableInside[activeWindow][sortedBgIx]) ||
                        (activeWindow == 2 && _windows.ObjWindowBgEnable[sortedBgIx]))
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

            // Work out what are the top target palette entries and determine
            // if those targets were enabled for color effects

            // Color effects are only going to be valid if they're enabled and
            // the window this pixel is in also has color effects enabled
            var colorEffectsValid = ColorEffects.SpecialEffect != SpecialEffect.None;
            var objHiddenByWindow = false;

            if (usingWindows)
            {
                colorEffectsValid &= ((activeWindow == -1 && _windows.ColorSpecialEffectEnableOutside) ||
                    ((activeWindow == 0 || activeWindow == 1) && _windows.ColorSpecialEffectEnableInside[activeWindow]) ||
                    (activeWindow == 2 && _windows.ObjWindowColorSpecialEffect));

                objHiddenByWindow = ((activeWindow == -1 && !_windows.ObjEnableOutside) ||
                    ((activeWindow == 0 || activeWindow == 1) && !_windows.ObjEnableInside[activeWindow]) ||
                    (activeWindow == 2 && !_windows.ObjWindowObjEnable));
            }

            var colorEffectUsed = ColorEffects.SpecialEffect;

            // Object layer used tracks whether the obj value has already been
            // used for this pixel
            var objLayerUsed = false;

            // BG layer ix is incremented to 1 if the highest priority
            // background has been used for this pixel already
            var bgLayerIx = 0;
            for (var target = 0; target < (ColorEffects.SpecialEffect == SpecialEffect.AlphaBlend ? 2 : 1); target++)
            {
                if (bgEntry.ColorIsPaletteIndex[bgLayerIx])
                {
                    PaletteEntry paletteEntry;
                    if (objLayerUsed // The OBJ layer has already been used for this pixel
                        || ((spriteEntry.PaletteColor & 0b1111) == 0) // The sprite is transparent on this pixel
                        || (bgEntry.Priority[target] < spriteEntry.Priority) // The background is higher priority
                        || objHiddenByWindow) // Windows are enabled and this pixel is in a window state that means no obj
                    {
                        if (bgEntry.PaletteColor[bgLayerIx] == 0)
                        {
                            paletteEntry = BackdropColor;
                            colorEffectsValid &= ColorEffects.TargetBackdrop[target];
                        }
                        else
                        {
                            paletteEntry = _paletteEntries[bgEntry.PaletteColor[bgLayerIx]];
                            colorEffectsValid &= ColorEffects.TargetBg[target][bgEntry.BgId[bgLayerIx]];
                        }
                        bgLayerIx++;
                    }
                    else
                    {
                        paletteEntry = _paletteEntries[0x100 + spriteEntry.PaletteColor];

                        // Semi transparent sprites
                        if (spriteEntry.PixelMode == SpriteMode.SemiTransparent)
                        {
                            colorEffectUsed = SpecialEffect.AlphaBlend;
                        }
                        else
                        {
                            colorEffectsValid &= ColorEffects.TargetObj[target];
                        }

                        objLayerUsed = true;
                    }

                    Utils.ColorToRgb(paletteEntry, _pixels[target]);
                }
                else
                {
                    if (objLayerUsed // The OBJ layer has already been used for this pixel
                        || ((spriteEntry.PaletteColor & 0b1111) == 0) // The sprite is transparent on this pixel
                        || (bgEntry.Priority[target] < spriteEntry.Priority) // The background is higher priority
                        || objHiddenByWindow)
                    {
                        if (bgEntry.PaletteColor[bgLayerIx] == -1)
                        {
                            colorEffectsValid &= ColorEffects.TargetBackdrop[target];
                            Utils.ColorToRgb(BackdropColor, _pixels[target]);
                        }
                        else
                        {
                            colorEffectsValid &= ColorEffects.TargetBg[target][bgEntry.BgId[bgLayerIx]];
                            Utils.ColorToRgb(bgEntry.PaletteColor[bgLayerIx], _pixels[target]);
                        }
                        bgLayerIx++;
                    }
                    else
                    {
                        // Semi transparent sprites
                        if (spriteEntry.PixelMode == SpriteMode.SemiTransparent)
                        {
                            colorEffectUsed = SpecialEffect.AlphaBlend;
                        }
                        else
                        {
                            colorEffectsValid &= ColorEffects.TargetObj[target];
                        }

                        Utils.ColorToRgb(_paletteEntries[0x100 + spriteEntry.PaletteColor], _pixels[target]);
                        objLayerUsed = true;
                    }
                }
            }

            if (!colorEffectsValid || colorEffectUsed == SpecialEffect.None)
            {
                // Just copy the highest priority pixel into the frame buffer as no color effects to apply
                Array.Copy(_pixels[0], 0, FrameBuffer, fbPtr, 4);
            }
            else
            {
                switch (colorEffectUsed)
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
        if (Backgrounds[sortedBgIxs[1]].Control.BgPriority >= Backgrounds[sortedBgIxs[2]].Control.BgPriority && sortedBgIxs[1] > sortedBgIxs[2])
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
                if (sprite.ObjMode != SpriteMode.ObjWindow && _objBuffer[lineX].Priority <= sprite.PriorityRelativeToBg && (_objBuffer[lineX].PaletteColor & 0b1111) != 0) continue;

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

                if (sprite.ObjMode == SpriteMode.ObjWindow)
                {
                    if (pixelPalNo == 0) continue; // Transparent pixels don't count towards obj window

                    _windowState[lineX] = 2;
                }
                else
                {
                    _objBuffer[lineX].PaletteColor = pixelPalNo;
                    _objBuffer[lineX].Priority = sprite.PriorityRelativeToBg;
                    _objBuffer[lineX].PixelMode = sprite.ObjMode;
                }
            }
        }
    }

    private void DrawBlankScanline()
    {
        var fbPtr = Device.WIDTH * 4 * CurrentLine; // 4 bytes per pixel
        for (var x = 0; x < Device.WIDTH; x++)
        {
            Utils.ColorToRgb(BackdropColor, FrameBuffer.AsSpan(fbPtr));
            fbPtr += 4;
        }
    }

    private void DrawProhibitedModeScanline()
    {
        for (var ii = 0; ii < _scanlineBgBuffer.Length; ii++)
        {
            Array.Clear(_scanlineBgBuffer, 0, _scanlineBgBuffer.Length);
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
        var baseRefX = background.RefPointXLatched;
        var baseRefY = background.RefPointYLatched;

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
                BgSize.Regular32x64 => (screenBlockTileY / 32),
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

    private void DrawBitmapBgModeScanlineCommon(Background background, int width, int height, uint baseAddress, ref int[] scanlineBgBuffer, int backdropColor)
    {
        var baseRefX = background.RefPointXLatched;
        var baseRefY = background.RefPointYLatched;

        for (var pixel = 0; pixel < Device.WIDTH; pixel++)
        {
            var xBase = baseRefX >> 8;
            var yBase = baseRefY >> 8;
            baseRefX += background.Dx;
            baseRefY += background.Dy;

            // Affine backgrounds can either make things outside the area transparent or wrap for overflow
            if (background.Control.DisplayAreaOverflow)
            {
                if (xBase >= width) xBase %= width;
                else if (xBase < 0) xBase = width + (xBase % width);

                if (yBase >= height) yBase %= height;
                else if (yBase < 0) yBase = height + (yBase % height);
            }
            else if (xBase < 0 || xBase >= width || yBase < 0 || yBase >= height)
            {
                scanlineBgBuffer[pixel] = backdropColor;
                continue;
            }

            switch (Dispcnt.BgMode)
            {
                case BgMode.Video3:
                    {
                        var address = baseAddress + (yBase * 480) + (xBase * 2);
                        var color = Vram[address] | (Vram[address + 1] << 8);
                        scanlineBgBuffer[pixel] = color;
                    }
                    break;
                case BgMode.Video4:
                    {
                        var address = baseAddress + (yBase * 240) + xBase;
                        var paletteIndex = Vram[address];
                        scanlineBgBuffer[pixel] = paletteIndex;
                    }
                    break;
                case BgMode.Video5:
                    {
                        var address = baseAddress + (yBase * 320) + (xBase * 2);
                        var color = Vram[address] | (Vram[address + 1] << 8);
                        scanlineBgBuffer[pixel] = color;
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private void DrawBgMode3CurrentScanline(ref int[] scanlineBgBuffer)
    {
        DrawBitmapBgModeScanlineCommon(Backgrounds[2], Device.WIDTH, Device.HEIGHT, 0, ref scanlineBgBuffer, -1);
    }

    private void DrawBgMode4CurrentScanline(ref int[] scanlineBgBuffer)
    {
        // BG Mode 4/5 have a double buffer that is switched using Frame1Select on DISPCNT
        var baseAddress = Dispcnt.Frame1Select ? 0x0000_A000u : 0x0;

        DrawBitmapBgModeScanlineCommon(Backgrounds[2], Device.WIDTH, Device.HEIGHT, baseAddress, ref scanlineBgBuffer, 0);
    }

    private void DrawBgMode5CurrentScanline(ref int[] scanlineBgBuffer)
    {
        // Screen is 160*128 in this mode, the rest gets backdrop color
        var baseAddress = Dispcnt.Frame1Select ? 0x0000_A000u : 0x0;

        DrawBitmapBgModeScanlineCommon(Backgrounds[2], 160, 128, baseAddress, ref scanlineBgBuffer, -1);
    }
}
