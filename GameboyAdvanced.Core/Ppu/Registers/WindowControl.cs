namespace GameboyAdvanced.Core.Ppu.Registers;

internal struct WindowControl
{
    internal bool Win0BG0Enable;
    internal bool Win0BG1Enable;
    internal bool Win0BG2Enable;
    internal bool Win0BG3Enable;
    internal bool Win0ObjEnable;
    internal bool Win0ColorSpecialEffect;

    internal bool Win1BG0Enable;
    internal bool Win1BG1Enable;
    internal bool Win1BG2Enable;
    internal bool Win1BG3Enable;
    internal bool Win1ObjEnable;
    internal bool Win1ColorSpecialEffect;

    internal ushort Get() => (ushort)
        ((Win0BG0Enable ? 1 << 0 : 0) |
         (Win0BG1Enable ? 1 << 1 : 0) |
         (Win0BG2Enable ? 1 << 2 : 0) |
         (Win0BG3Enable ? 1 << 3 : 0) |
         (Win0ObjEnable ? 1 << 4 : 0) |
         (Win0ColorSpecialEffect ? 1 << 5 : 0) |
         (Win1BG0Enable ? 1 << 8 : 0) |
         (Win1BG1Enable ? 1 << 9 : 0) |
         (Win1BG2Enable ? 1 << 10 : 0) |
         (Win1BG3Enable ? 1 << 11 : 0) |
         (Win1ObjEnable ? 1 << 12 : 0) |
         (Win1ColorSpecialEffect ? 1 << 13 : 0));

    internal void Set(ushort value)
    {
        Win0BG0Enable = (value & 1 << 0) != 0;
        Win0BG1Enable = (value & 1 << 1) != 0;
        Win0BG2Enable = (value & 1 << 2) != 0;
        Win0BG3Enable = (value & 1 << 3) != 0;
        Win0ObjEnable = (value & 1 << 4) != 0;
        Win0ColorSpecialEffect = (value & 1 << 5) != 0;
        Win1BG0Enable = (value & 1 << 8) != 0;
        Win1BG1Enable = (value & 1 << 9) != 0;
        Win1BG2Enable = (value & 1 << 10) != 0;
        Win1BG3Enable = (value & 1 << 11) != 0;
        Win1ObjEnable = (value & 1 << 12) != 0;
        Win1ColorSpecialEffect = (value & 1 << 13) != 0;
    }
}
