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
internal struct Mosaic
{
    internal int BGHSize;
    internal int BGVSize;
    internal int ObjHSize;
    internal int ObjVSize;

    internal ushort Get() =>
        (ushort)(BGHSize | BGVSize << 4 | ObjHSize << 8 | ObjVSize << 12);

    internal void Set(ushort value)
    {
        BGHSize = value & 0b1111;
        BGVSize = value >> 4 & 0b1111;
        ObjHSize = value >> 8 & 0b1111;
        ObjVSize = value >> 12 & 0b1111;
    }

    internal void Reset()
    {
        BGHSize = 0;
        BGVSize = 0;
        ObjHSize = 0;
        ObjVSize = 0;
    }
}
