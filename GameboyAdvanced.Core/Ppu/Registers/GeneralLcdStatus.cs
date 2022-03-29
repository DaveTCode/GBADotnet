namespace GameboyAdvanced.Core.Ppu.Registers;

/// <summary>
/// The DISPSTAT register containing the current status of the video controller
/// such as HBlank/VBlank state and which interrupt types are enabled.
/// 
/// Partially writable.
/// </summary>
public struct GeneralLcdStatus
{
    public bool VBlankFlag;
    public bool HBlankFlag;
    public bool VCounterFlag;
    public bool VBlankIrqEnable;
    public bool HBlankIrqEnable;
    public bool VCounterIrqEnable;
    public ushort VCountSetting;

    internal ushort Read() => (ushort)
        ((VBlankFlag ? 0b1 : 0u) |
         (HBlankFlag ? 0b10 : 0u) |
         (VCounterFlag ? 0b100 : 0u) |
         (VBlankIrqEnable ? 0b1000 : 0u) |
         (HBlankIrqEnable ? 0b1_0000 : 0u) |
         (VCounterIrqEnable ? 0b10_0000 : 0u) |
         // bits 6,7 unused
         ((uint)VCountSetting << 8)
        );

    internal void Update(ushort value)
    {
        UpdateB1((byte)value);
        VCountSetting = (ushort)(value >> 8);
    }

    internal void UpdateB1(byte value)
    {
        VBlankIrqEnable = (value & 0b1000) == 0b1000;
        HBlankIrqEnable = (value & 0b1_0000) == 0b1_0000;
        VCounterIrqEnable = (value & 0b10_0000) == 0b10_0000;
    }
}
