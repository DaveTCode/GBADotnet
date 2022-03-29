namespace GameboyAdvanced.Core.Ppu.Registers;

/// <summary>
/// The mosiac register provides configuration of the mosaic function on any 
/// of BG0-3. Whether mosaic is enabled on a specific BG is controller by the
/// background control register.
/// 
/// GBATek Note:
/// "Example: When setting H-Size to 5, then pixels 0-5 of each display row 
/// are colorized as pixel 0, pixels 6-11 as pixel 6, pixels 12-17 as pixel 
/// 12, and so on."
/// </summary>
public struct Mosaic
{
    public int BGHSize;
    public int BGVSize;
    public int ObjHSize;
    public int ObjVSize;

    internal ushort Get() =>
        (ushort)(BGHSize | (BGVSize << 4) | (ObjHSize << 8) | (ObjVSize << 12));

    internal void UpdateB1(byte value)
    {
        BGHSize = value & 0b1111;
        BGVSize = (value >> 4) & 0b1111;
    }

    internal void UpdateB2(byte value)
    {
        ObjHSize = value & 0b1111;
        ObjVSize = (value >> 4) & 0b1111;
    }

    internal void Reset()
    {
        BGHSize = 0;
        BGVSize = 0;
        ObjHSize = 0;
        ObjVSize = 0;
    }
}
