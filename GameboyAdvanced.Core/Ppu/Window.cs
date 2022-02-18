namespace GameboyAdvanced.Core.Ppu;

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
        ((Win0BG0Enable ? (1 << 0) : 0) |
         (Win0BG1Enable ? (1 << 1) : 0) |
         (Win0BG2Enable ? (1 << 2) : 0) |
         (Win0BG3Enable ? (1 << 3) : 0) |
         (Win0ObjEnable ? (1 << 4) : 0) |
         (Win0ColorSpecialEffect ? (1 << 5) : 0) |
         (Win1BG0Enable ? (1 << 8) : 0) |
         (Win1BG1Enable ? (1 << 9) : 0) |
         (Win1BG2Enable ? (1 << 10) : 0) |
         (Win1BG3Enable ? (1 << 11) : 0) |
         (Win1ObjEnable ? (1 << 12) : 0) |
         (Win1ColorSpecialEffect ? (1 << 12) : 0));

    internal void Set(ushort value)
    {
        Win0BG0Enable = (value & (1 << 0)) == 1;
        Win0BG1Enable = (value & (1 << 1)) == 1;
        Win0BG2Enable = (value & (1 << 2)) == 1;
        Win0BG3Enable = (value & (1 << 3)) == 1;
        Win0ObjEnable = (value & (1 << 4)) == 1;
        Win0ColorSpecialEffect = (value & (1 << 5)) == 1;
        Win1BG0Enable = (value & (1 << 8)) == 1;
        Win1BG1Enable = (value & (1 << 9)) == 1;
        Win1BG2Enable = (value & (1 << 10)) == 1;
        Win1BG3Enable = (value & (1 << 11)) == 1;
        Win1ObjEnable = (value & (1 << 12)) == 1;
        Win1ColorSpecialEffect = (value & (1 << 13)) == 1;
    }
}

internal struct Window
{
    internal int Index;
    internal int X1;
    internal int X2;
    internal int Y1;
    internal int Y2;

    internal Window(int index)
    {
        Index = index;
        X1 = 0;
        X2 = 0;
        Y1 = 0;
        Y2 = 0;
    }

    internal void Reset()
    {
        X1 = 0;
        X2 = 0;
        Y1 = 0;
        Y2 = 0;
    }

    /// <summary>
    /// WIN[01]H sets the start and end of the window in a single half word 
    /// write.
    /// 
    /// The values have interesting capping behaviour, X2 gets capped at 
    /// Device.WIDTH but the behaviour of X1 if it is > X2 depends on the 
    /// value of X2.
    /// </summary>
    /// <remarks>
    /// Behaviour here was borrowed knowledge from mgba.
    /// </remarks>
    internal void SetX(ushort value)
    {
        X1 = value >> 8;
        X2 = (value & 0xFF);

        if (X1 > Device.WIDTH && X1 > X2)
        {
            X1 = 0;
        }
        if (X2 > Device.WIDTH)
        {
            X2 = Device.WIDTH;
            if (X1 > Device.WIDTH)
            {
                X1 = Device.WIDTH;
            }
        }
    }

    internal void SetY(ushort value)
    {
        Y1 = value >> 8;
        Y2 = (value & 0xFF);

        if (Y1 > Device.HEIGHT && Y1 > Y2)
        {
            Y1 = 0;
        }
        if (Y2 > Device.HEIGHT)
        {
            Y2 = Device.HEIGHT;
            if (Y1 > Device.HEIGHT)
            {
                Y1 = Device.HEIGHT;
            }
        }
    }
}
