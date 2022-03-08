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

internal struct Sprite
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
    }

    internal void UpdateAttr2(ushort value)
    {
        X = value & 0b1_1111_1111;
        RotationScalingParameter = (value >> 9) & 0b1_1111;
        HorizontalFlip = ((value >> 12) & 0b1) == 0b1;
        VerticalFlip = ((value >> 13) & 0b1) == 0b1;
        SpriteSize = (value >> 14) & 0b11;
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

    public override string ToString() => $"{Index} - ({X},{Y}) = Tile {Tile}";
}
