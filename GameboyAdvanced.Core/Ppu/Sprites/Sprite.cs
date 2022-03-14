namespace GameboyAdvanced.Core.Ppu;

internal enum SpriteMode
{
    Normal,
    SemiTransparent,
    ObjWindow,
    Prohibited,
}

internal enum SpriteShape
{
    Square,
    Horizontal,
    Vertical,
    Prohibited,
}

internal class Sprite
{
    internal int Index;
    internal int X;
    internal int Y;
    internal bool RotationScalingFlag;
    internal int RotationScalingParameter;
    internal bool DoubleSize;
    internal bool ObjDisable;
    internal SpriteMode ObjMode;
    internal SpriteShape Shape;
    internal bool ObjMosaic;
    internal bool LargePalette;
    internal bool HorizontalFlip;
    internal bool VerticalFlip;
    internal int SpriteSize;
    internal int Tile;
    internal int PriorityRelativeToBg;
    internal int PaletteNumber;

    /// <summary>
    /// This is a calculated property of a sprite based on size and shape
    /// cached to make determining whether a sprite is within a scanline 
    /// more efficient.
    /// </summary>
    internal int Height;

    /// <summary>
    /// This is a calculated property of a sprite based on size and shape
    /// cached to make determining whether a sprite is within a scanline 
    /// more efficient.
    /// </summary>
    internal int Width;

    internal void UpdateAttr1(ushort value)
    {
        Y = value & 0b1111_1111;
        RotationScalingFlag = ((value >> 8) & 0b1) == 0b1;
        if (RotationScalingFlag)
        {
            DoubleSize = ((value >> 9) & 0b1) == 0b1;
            ObjDisable = false;
        }
        else
        {
            DoubleSize = false;
            ObjDisable = ((value >> 9) & 0b1) == 0b1;
        }
        ObjMode = (SpriteMode)((value >> 10) & 0b11);
        ObjMosaic = ((value >> 12) & 0b1) == 0b1;
        LargePalette = ((value >> 13) & 0b1) == 0b1;
        Shape = (SpriteShape)((value >> 14) & 0b11);
        UpdateSize();
    }

    internal void UpdateAttr2(ushort value)
    {
        X = value & 0b1_1111_1111;
        RotationScalingParameter = (value >> 9) & 0b1_1111;
        HorizontalFlip = ((value >> 12) & 0b1) == 0b1;
        VerticalFlip = ((value >> 13) & 0b1) == 0b1;
        SpriteSize = (value >> 14) & 0b11;
        UpdateSize();
    }

    internal void UpdateAttr3(ushort value)
    {
        Tile = value & 0b11_1111_1111;
        PriorityRelativeToBg = (value >> 10) & 0b11;
        PaletteNumber = (value >> 12) & 0b1111;
    }

    internal void Reset()
    {
        UpdateAttr1(0);
        UpdateAttr2(0);
        UpdateAttr3(0);
    }

    private void UpdateSize()
    {
        (Width, Height) = SpriteSize switch
        {
            0 => Shape switch
            {
                SpriteShape.Square => (8, 8),
                SpriteShape.Horizontal => (16, 8),
                SpriteShape.Vertical => (8, 16),
                _ => (0, 0),
            },
            1 => Shape switch
            {
                SpriteShape.Square => (16, 16),
                SpriteShape.Horizontal => (32, 8),
                SpriteShape.Vertical => (8, 32),
                _ => (0, 0),
            },
            2 => Shape switch
            {
                SpriteShape.Square => (32, 32),
                SpriteShape.Horizontal => (32, 16),
                SpriteShape.Vertical => (16, 32),
                _ => (0, 0),
            },
            3 => Shape switch
            {
                SpriteShape.Square => (64, 64),
                SpriteShape.Horizontal => (64, 32),
                SpriteShape.Vertical => (64, 32),
                _ => (0, 0),
            },
            _ => throw new Exception("Invalid sprite size")
        };
    }

    public override string ToString() => $"{Index} - ({X},{Y}) = Tile {Tile}";
}
