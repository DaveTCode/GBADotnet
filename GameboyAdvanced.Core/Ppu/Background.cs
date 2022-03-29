using GameboyAdvanced.Core.Ppu.Registers;

namespace GameboyAdvanced.Core.Ppu;

/// <summary>
/// There are 4 available backgrounds in the GBA PPU (BG0-3) which behave 
/// slightly differently depending on the overall mode.
/// 
/// There's a great table in the Tonc docs (https://www.coranac.com/tonc/text/regbg.htm)
/// illustrating the various functions:
/// mode	BG0	BG1	BG2	BG3
/// 0	    reg reg reg reg
/// 1	    reg reg aff	-
/// 2	    -	-	aff aff
/// 
/// That is, when the overall DISPCNT BgMode is 0 then all 4 backgrounds act
/// in regular (also known as text) mode which means they use the X/YOffset
/// insteda of RefPointX/Y and friends.
/// </summary>
public class Background
{
    /// <summary>
    /// Just used to disambiguate backgrounds for debugging
    /// </summary>
    public int Index;

    /// <summary>
    /// Control register used to specify the behaviour of the background
    /// <see cref="BgControlReg"/>
    /// </summary>
    public BgControlReg Control;

    /// <summary>
    /// XOffset is used to scroll the backgroung by specifying which xcoord 
    /// should go on the left of the screen.
    /// </summary>
    public int XOffset;

    /// <summary>
    /// YOffset is used to scroll the backgroung by specifying which ycoord 
    /// should go on the top of the screen.
    /// </summary>
    public int YOffset;

    // These fields are all specific to affine transforms on BG2/3 but because
    // we're being obnoxiously efficient with coding we aren't using an
    // inheritance heirachy to avoid virtual function calls so they're stored
    // but unused on all backgrounds
    public int RefPointX;
    public int RefPointY;
    public short Dx;
    public short Dmx;
    public short Dy;
    public short Dmy;

    internal Background(int index)
    {
        Index = index;
        Reset();
    }

    internal void Reset()
    {
        XOffset = 0;
        YOffset = 0;
        Control.Reset();
        RefPointX = 0;
        RefPointY = 0;
        Dx = 0;
        Dmx = 0;
        Dy = 0;
        Dmy = 0;
    }

    internal void UpdateReferencePointX(byte value, int byteIndex, uint mask)
    {
        var newVal = (RefPointX & mask) | (uint)(value << (byteIndex * 8));

        // 28 bit signed integers, so if 27th bit is set then make negative
        if ((newVal & (1 << 27)) == (1 << 27))
        {
            newVal |= 0xF000_0000;
        }

        RefPointX = (int)newVal;
    }

    internal void UpdateReferencePointY(byte value, int byteIndex, uint mask)
    {
        var newVal = (RefPointY & mask) | (uint)(value << (byteIndex * 8));

        // 28 bit signed integers, so if 27th bit is set then make negative
        if ((newVal & (1 << 27)) == (1 << 27))
        {
            newVal |= 0xF000_0000;
        }

        RefPointY = (int)newVal;
    }
}
