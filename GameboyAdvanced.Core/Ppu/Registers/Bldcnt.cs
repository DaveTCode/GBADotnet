namespace GameboyAdvanced.Core.Ppu.Registers;

internal enum ColorSpecialEffect
{
    None = 0b00,
    AlphaBlend = 0b01,
    BrightnessInc = 0b10,
    BrightnessDec = 0b11,
}

public struct Bldcnt
{
    internal bool[] Bg1stTargetPixel = new bool[4];
    internal bool Obj1stTargetPixel = false;
    internal bool Bd1stTargetPixel = false;
    internal ColorSpecialEffect ColorSpecialEffect = ColorSpecialEffect.None;
    internal bool[] Bg2ndTargetPixel = new bool[4];
    internal bool Obj2ndTargetPixel = false;
    internal bool Bd2ndTargetPixel = false;

    public Bldcnt()
    {
    }

    internal void Reset()
    {
        UpdateB1(0);
        UpdateB1(2);
    }

    internal void UpdateB2(byte val)
    {
        Bg2ndTargetPixel[0] = (val & (1 << 0)) != 0;
        Bg2ndTargetPixel[1] = (val & (1 << 1)) != 0;
        Bg2ndTargetPixel[2] = (val & (1 << 2)) != 0;
        Bg2ndTargetPixel[3] = (val & (1 << 3)) != 0;
        Obj2ndTargetPixel = (val & (1 << 4)) != 0;
        Bd2ndTargetPixel = (val & (1 << 5)) != 0;
    }

    internal void UpdateB1(byte val)
    {
        Bg1stTargetPixel[0] = (val & (1 << 0)) != 0;
        Bg1stTargetPixel[1] = (val & (1 << 1)) != 0;
        Bg1stTargetPixel[2] = (val & (1 << 2)) != 0;
        Bg1stTargetPixel[3] = (val & (1 << 3)) != 0;
        Obj1stTargetPixel = (val & (1 << 4)) != 0;
        Bd1stTargetPixel = (val & (1 << 5)) != 0;
        ColorSpecialEffect = (ColorSpecialEffect)((val >> 6) & 0b11);
    }

    internal ushort Get() =>
        (ushort)((Bg1stTargetPixel[0] ? 1 << 0 : 0)
            | (Bg1stTargetPixel[1] ? 1 << 1 : 0)
            | (Bg1stTargetPixel[2] ? 1 << 2 : 0)
            | (Bg1stTargetPixel[3] ? 1 << 3 : 0)
            | (Obj1stTargetPixel ? 1 << 4 : 0)
            | (Bd1stTargetPixel ? 1 << 5 : 0)
            | ((ushort)ColorSpecialEffect << 6)
            | (Bg2ndTargetPixel[0] ? 1 << 8 : 0)
            | (Bg2ndTargetPixel[1] ? 1 << 9 : 0)
            | (Bg2ndTargetPixel[2] ? 1 << 10 : 0)
            | (Bg2ndTargetPixel[3] ? 1 << 11 : 0)
            | (Obj2ndTargetPixel ? 1 << 12 : 0)
            | (Bd2ndTargetPixel ? 1 << 13 : 0));
}
