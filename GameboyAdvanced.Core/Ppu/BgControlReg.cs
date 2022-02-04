namespace GameboyAdvanced.Core.Ppu;

internal struct BgControlReg
{
    internal int BgPriority;
    internal int CharBaseBlock;
    internal bool IsMosaic;
    internal bool LargePalette;
    internal int ScreenBaseBlock;
    internal bool DisplayAreaOverflow;
    internal int ScreenSize;

    internal ushort Read() => (ushort)
        (BgPriority |
         (CharBaseBlock << 2) |
         (IsMosaic ? (1 << 6) : 0) |
         (LargePalette ? (1 << 7) : 0) |
         (ScreenBaseBlock << 8) |
         (DisplayAreaOverflow ? (1 << 13) : 0) |
         (ScreenSize << 14));

    internal void Update(ushort value)
    {
        BgPriority = value & 0b11;
        CharBaseBlock = (value >> 2) & 0b11;
        IsMosaic = ((value >> 7) & 0b1) == 0b1;
        LargePalette = ((value >> 7) & 0b1) == 0b1;
        ScreenBaseBlock = (value >> 8) & 0b1_1111;
        DisplayAreaOverflow = ((value >> 13) & 0b1) == 0b1;
        ScreenSize = (value >> 14) & 0b11;
    }
}
