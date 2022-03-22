using GameboyAdvanced.Core.Ppu.Registers;

namespace GameboyAdvanced.Core.Ppu;

public class Windows
{
    public byte[] X1 = new byte[2];
    public byte[] X2 = new byte[2];
    public byte[] Y1 = new byte[2];
    public byte[] Y2 = new byte[2];
    public bool[][] BgEnableInside = new bool[2][]
    {
        new bool[4], new bool[4]
    };
    public bool[] ObjEnableInside = new bool[2];
    public bool[] ColorSpecialEffectEnableInside = new bool[2];

    public bool[] BgEnableOutside = new bool[4];
    public bool ObjEnableOutside;
    public bool ColorSpecialEffectEnableOutside;

    public bool[] ObjWindowBgEnable = new bool[4];
    public bool ObjWindowObjEnable;
    public bool ObjWindowColorSpecialEffect;

    internal Windows()
    {
        Reset();
    }

    internal void Reset()
    {
        Array.Clear(X1);
        Array.Clear(X2);
        Array.Clear(Y1);
        Array.Clear(Y2);
        
        Array.Clear(BgEnableInside[0]);
        Array.Clear(BgEnableInside[1]);
        Array.Clear(ObjEnableInside);
        Array.Clear(ColorSpecialEffectEnableInside);

        Array.Clear(BgEnableOutside);
        ObjEnableOutside = false;
        ColorSpecialEffectEnableOutside = false;

        Array.Clear(ObjWindowBgEnable);
        ObjWindowObjEnable = false;
        ObjWindowColorSpecialEffect = false;
    }

    internal void SetXB1(int window, byte value)
    {
        X2[window] = (byte)(value - 1);
        CapX(window);
    }

    internal void SetXB2(int window, byte value)
    {
        X1[window] = value;
        CapX(window);
    }

    internal void SetYB1(int window, byte value)
    {
        Y2[window] = (byte)(value - 1);
        CapY(window);
    }

    internal void SetYB2(int window, byte value)
    {
        Y1[window] = value;
        CapY(window);
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
    internal void CapX(int window)
    {
        if (X1[window] > Device.WIDTH && X1[window] > X2[window])
        {
            X1[window] = 0;
        }
        if (X2[window] > Device.WIDTH)
        {
            X2[window] = Device.WIDTH;
            if (X1[window] > Device.WIDTH)
            {
                X1[window] = Device.WIDTH;
            }
        }
    }

    internal void CapY(int window)
    {
        if (Y1[window] > Device.HEIGHT && Y1[window] > Y2[window])
        {
            Y1[window] = 0;
        }
        if (Y2[window] > Device.HEIGHT)
        {
            Y2[window] = Device.HEIGHT;
            if (Y1[window] > Device.HEIGHT)
            {
                Y1[window] = Device.HEIGHT;
            }
        }
    }

    internal int GetHighestPriorityWindow(DisplayCtrl dispCnt, int x, int y)
    {
        if (x >= X1[0] && x < X2[0] && y >= Y1[0] && y < Y2[0] && dispCnt.Window0Display)
        {
            return 0;
        }
        else if (x >= X1[1] && x < X2[1] && y >= Y1[1] && y < Y2[1] && dispCnt.Window1Display)
        {
            return 1;
        }
        
        return -1;
    }

    internal void UpdateWinIn(int window, byte value)
    {
        BgEnableInside[window][0] = (value & (1 << 0)) == (1 << 0);
        BgEnableInside[window][1] = (value & (1 << 1)) == (1 << 1);
        BgEnableInside[window][2] = (value & (1 << 2)) == (1 << 2);
        BgEnableInside[window][3] = (value & (1 << 3)) == (1 << 3);
        ObjEnableInside[window] = (value & (1 << 4)) == (1 << 4);
        ColorSpecialEffectEnableInside[window] = (value & (1 << 5)) == (1 << 5);
    }

    internal void UpdateWinOutB1(byte value)
    {
        BgEnableOutside[0] = (value & (1 << 0)) == (1 << 0);
        BgEnableOutside[1] = (value & (1 << 1)) == (1 << 1);
        BgEnableOutside[2] = (value & (1 << 2)) == (1 << 2);
        BgEnableOutside[3] = (value & (1 << 3)) == (1 << 3);
        ObjEnableOutside = (value & (1 << 4)) == (1 << 4);
        ColorSpecialEffectEnableOutside = (value & (1 << 5)) == (1 << 5);
    }

    internal void UpdateWinOutB2(byte value)
    {
        ObjWindowBgEnable[0] = (value & (1 << 0)) == (1 << 0);
        ObjWindowBgEnable[1] = (value & (1 << 1)) == (1 << 1);
        ObjWindowBgEnable[2] = (value & (1 << 2)) == (1 << 2);
        ObjWindowBgEnable[3] = (value & (1 << 3)) == (1 << 3);
        ObjWindowObjEnable = (value & (1 << 4)) == (1 << 4);
        ObjWindowColorSpecialEffect = (value & (1 << 5)) == (1 << 5);
    }

    internal ushort GetWinIn() => (ushort)
        ((BgEnableInside[0][0] ? 1 << 0 : 0) |
         (BgEnableInside[0][1] ? 1 << 1 : 0) |
         (BgEnableInside[0][2] ? 1 << 2 : 0) |
         (BgEnableInside[0][3] ? 1 << 3 : 0) |
         (ObjEnableInside[0] ? 1 << 4 : 0) |
         (ColorSpecialEffectEnableInside[0] ? 1 << 5 : 0) |
         (BgEnableInside[1][0] ? 1 << 8 : 0) |
         (BgEnableInside[1][1] ? 1 << 9 : 0) |
         (BgEnableInside[1][2] ? 1 << 10 : 0) |
         (BgEnableInside[1][3] ? 1 << 11 : 0) |
         (ObjEnableInside[1] ? 1 << 12 : 0) |
         (ColorSpecialEffectEnableInside[1] ? 1 << 13 : 0));

    internal ushort GetWinOut() => (ushort)
        ((BgEnableOutside[0] ? 1 << 0 : 0) |
         (BgEnableOutside[1] ? 1 << 1 : 0) |
         (BgEnableOutside[2] ? 1 << 2 : 0) |
         (BgEnableOutside[3] ? 1 << 3 : 0) |
         (ObjEnableOutside ? 1 << 4 : 0) |
         (ColorSpecialEffectEnableOutside ? 1 << 5 : 0) |
         (ObjWindowBgEnable[0] ? 1 << 8 : 0) |
         (ObjWindowBgEnable[1] ? 1 << 9 : 0) |
         (ObjWindowBgEnable[2] ? 1 << 10 : 0) |
         (ObjWindowBgEnable[3] ? 1 << 11 : 0) |
         (ObjWindowObjEnable ? 1 << 12 : 0) |
         (ObjWindowColorSpecialEffect ? 1 << 13 : 0)
        );
}
