namespace GameboyAdvanced.Core.Ppu.Registers;

public enum BgMode
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

public struct DisplayCtrl
{
    public BgMode BgMode;
    public bool IsCGB;
    public bool Frame1Select;
    public bool AllowOamDuringHblank;
    public bool OneDimObjCharVramMapping;
    public bool ForcedBlank;
    public bool[] ScreenDisplayBg;
    public bool ScreenDisplayObj;
    public bool[] WindowDisplay;
    public bool ObjWindowDisplay;

    public DisplayCtrl()
    {
        BgMode = BgMode.Video0;
        IsCGB = false;
        Frame1Select = false;
        AllowOamDuringHblank = false;
        OneDimObjCharVramMapping = false;
        ForcedBlank = false;
        ScreenDisplayBg = new bool[4];
        ScreenDisplayObj = false;
        WindowDisplay = new bool[2];
        ObjWindowDisplay = false;
    }

    internal void Reset()
    {
        Update(0);
    }

    internal void Update(ushort value)
    {
        UpdateB1((byte)value);
        UpdateB2((byte)(value >> 8));
    }

    internal void UpdateB1(byte value)
    {
        BgMode = (BgMode)(value & 0b111);
        IsCGB = (value & 0b1000) == 0b1000;
        Frame1Select = (value & 0b1_0000) == 0b1_0000;
        AllowOamDuringHblank = (value & 0b10_0000) == 0b10_0000;
        OneDimObjCharVramMapping = (value & 0b100_0000) == 0b100_0000;
        ForcedBlank = (value & 0b1000_0000) == 0b1000_0000;
    }

    internal void UpdateB2(byte value)
    {
        ScreenDisplayBg[0] = (value & 0b1) == 0b1;
        ScreenDisplayBg[1] = (value & 0b10) == 0b10;
        ScreenDisplayBg[2] = (value & 0b100) == 0b100;
        ScreenDisplayBg[3] = (value & 0b1000) == 0b1000;
        ScreenDisplayObj = (value & 0b1_0000) == 0b1_0000;
        WindowDisplay[0] = (value & 0b10_0000) == 0b10_0000;
        WindowDisplay[1] = (value & 0b100_0000) == 0b100_0000;
        ObjWindowDisplay = (value & 0b1000_0000) == 0b1000_0000;
    }

    internal ushort Read() => (ushort)
        ((ushort)BgMode |
        (IsCGB ? 1 << 3 : 0) |
        (Frame1Select ? 1 << 4 : 0) |
        (AllowOamDuringHblank ? 1 << 5 : 0) |
        (OneDimObjCharVramMapping ? 1 << 6 : 0) |
        (ForcedBlank ? 1 << 7 : 0) |
        (ScreenDisplayBg[0] ? 1 << 8 : 0) |
        (ScreenDisplayBg[1] ? 1 << 9 : 0) |
        (ScreenDisplayBg[2] ? 1 << 10 : 0) |
        (ScreenDisplayBg[3] ? 1 << 11 : 0) |
        (ScreenDisplayObj ? 1 << 12 : 0) |
        (WindowDisplay[0] ? 1 << 13 : 0) |
        (WindowDisplay[1] ? 1 << 14 : 0) |
        (ObjWindowDisplay ? 1 << 15 : 0));
}
