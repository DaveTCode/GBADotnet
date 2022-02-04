namespace GameboyAdvanced.Core.Ppu;


internal enum BgMode
{
    Video0 = 0b000,
    Video1 = 0b001,
    Video2 = 0b010,
    Video3 = 0b011,
    Video4 = 0b100,
    Video5 = 0b101,
    Prohibited6 = 0b110,
    Prohibited7 = 0b111,
}

internal struct DisplayCtrl
{
    internal BgMode BgMode;
    internal bool IsCGB;
    internal bool Frame1Select;
    internal bool AllowOamDuringHblank;
    internal bool OneDimObjCharVramMapping;
    internal bool ForcedBlank;
    internal bool ScreenDisplayBg0;
    internal bool ScreenDisplayBg1;
    internal bool ScreenDisplayBg2;
    internal bool ScreenDisplayBg3;
    internal bool ScreenDisplayObj;
    internal bool Window0Display;
    internal bool Window1Display;
    internal bool ObjWindowDisplay;

    internal void Update(ushort value)
    {
        BgMode = (BgMode)(value & 0b111);
        IsCGB = (value & 0b1000) == 0b1000;
        Frame1Select = (value & 0b1_0000) == 0b1_0000;
        AllowOamDuringHblank = (value & 0b10_0000) == 0b10_0000;
        OneDimObjCharVramMapping = (value & 0b100_0000) == 0b100_0000;
        ForcedBlank = (value & 0b1000_0000) == 0b1000_0000;
        ScreenDisplayBg0 = (value & 0b1_0000_0000) == 0b1_0000_0000;
        ScreenDisplayBg1 = (value & 0b10_0000_0000) == 0b10_0000_0000;
        ScreenDisplayBg2 = (value & 0b100_0000_0000) == 0b100_0000_0000;
        ScreenDisplayBg3 = (value & 0b1000_0000_0000) == 0b1000_0000_0000;
        ScreenDisplayObj = (value & 0b1_0000_0000_0000) == 0b1_0000_0000_0000;
        Window0Display = (value & 0b10_0000_0000_0000) == 0b10_0000_0000_0000;
        Window1Display = (value & 0b100_0000_0000_0000) == 0b100_0000_0000_0000;
        ObjWindowDisplay = (value & 0b1000_0000_0000_0000) == 0b1000_0000_0000_0000;
    }

    internal ushort Read() => (ushort)
        ((ushort)BgMode |
        (IsCGB ? (1 << 3) : 0) |
        (Frame1Select ? (1 << 4) : 0) |
        (AllowOamDuringHblank ? (1 << 5) : 0) |
        (OneDimObjCharVramMapping ? (1 << 6) : 0) |
        (ForcedBlank ? (1 << 7) : 0) |
        (ScreenDisplayBg0 ? (1 << 8) : 0) |
        (ScreenDisplayBg1 ? (1 << 9) : 0) |
        (ScreenDisplayBg2 ? (1 << 10) : 0) |
        (ScreenDisplayBg3 ? (1 << 11) : 0) |
        (ScreenDisplayObj ? (1 << 12) : 0) |
        (Window0Display ? (1 << 13) : 0) |
        (Window1Display ? (1 << 14) : 0) |
        (ObjWindowDisplay ? (1 << 15) : 0));
}
