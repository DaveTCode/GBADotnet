using GameboyAdvanced.Core.Ppu.Registers;

namespace GameboyAdvanced.Core.Ppu;

/// <summary>
/// An (unused) renderer which processes the whole frame at a time.
/// 
/// Historical artifact of emulator from before scanline renderering was 
/// implemented.
/// </summary>
public partial class Ppu
{
    internal void RenderFrame()
    {
        if (Dispcnt.BgMode == BgMode.Video0)
        {
            foreach (var background in Backgrounds)
            {
                if (Dispcnt.ScreenDisplayBg[background.Index])
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
                            var tileMap = Vram[tileMapAddress] | (Vram[tileMapAddress + 1] << 8);
                            var tile = tileMap & 0b11_1111_1111;
                            var _horizontalFlip = ((tileMap >> 10) & 1) == 1;
                            var _verticalFlip = ((tileMap >> 11) & 1) == 1;
                            var paletteNumber = ((tileMap >> 12) & 0b1111) << 4;

                            var tileAddress = charMapBase + (tile * 32);
                            for (var b = 0; b < 32; b++)
                            {
                                var x = b % 4;
                                var y = b / 4;
                                var tileData = Vram[tileAddress + b];
                                var pixel1PalIx = tileData & 0b1111;
                                var pixel2PalIx = tileData >> 4;

                                // Color 0 in each BG/OBJ palette is transparent so replace with palette 0 color 0
                                var pixel1PalNo = (pixel1PalIx == 0) ? 0 : (paletteNumber | pixel1PalIx) * 2;
                                var pixel2PalNo = (pixel2PalIx == 0) ? 0 : (paletteNumber | pixel2PalIx) * 2;

                                // Each palette takes up 2
                                var pixel1Color = _paletteRam[pixel1PalNo] | (_paletteRam[pixel1PalNo + 1] << 8);
                                var pixel2Color = _paletteRam[pixel2PalNo] | (_paletteRam[pixel2PalNo + 1] << 8);
                                var fbPtr = frameBufferTileAddress + (x * 8) + (y * Device.WIDTH * 4);
                                Utils.ColorToRgb(pixel1Color, FrameBuffer.AsSpan(fbPtr));
                                Utils.ColorToRgb(pixel2Color, FrameBuffer.AsSpan(fbPtr + 4));
                            }
                        }
                    }
                }
            }
        }
        // TODO - BG mode 1-2
        else if (Dispcnt.BgMode == BgMode.Video3)
        {
            // TODO - This just hacks up a return of the vram buffer from mode 3 -> RGB instead of processing per pixel
            for (var row = 0; row < 160; row++)
            {
                for (var col = 0; col < 240; col++)
                {
                    var vramPtr = 2 * ((row * 240) + col);
                    var fbPtr = 2 * vramPtr;
                    var hw = Vram[vramPtr] | (Vram[vramPtr + 1] << 8);
                    Utils.ColorToRgb(hw, FrameBuffer.AsSpan(fbPtr));
                }
            }
        }
        else if (Dispcnt.BgMode == BgMode.Video4)
        {
            var baseAddress = Dispcnt.Frame1Select ? 0x0000_A000 : 0x0;

            // TODO - This just hacks up a return of the vram buffer from mode 4 -> palette -> RGB instead of processing per pixel
            for (var row = 0; row < 160; row++)
            {
                for (var col = 0; col < 240; col++)
                {
                    var vramPtr = ((row * 240) + col);
                    var fbPtr = 4 * vramPtr;
                    var paletteIndex = Vram[baseAddress + vramPtr] * 2; // 2 bytes per color in palette
                    var color = _paletteRam[paletteIndex] | (_paletteRam[paletteIndex + 1] << 8);
                    Utils.ColorToRgb(color, FrameBuffer.AsSpan(fbPtr));
                }
            }
        }
        else if (Dispcnt.BgMode == BgMode.Video5)
        {
            throw new NotImplementedException("BG Mode 5 not implemented");
        }
    }
}
