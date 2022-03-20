namespace GameboyAdvanced.Core.Ppu;

public struct Window
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
