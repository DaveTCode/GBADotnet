namespace GameboyAdvanced.Core.Ppu.Registers;

/// <summary>
/// The DISPSTAT register containing the current status of the video controller
/// such as HBlank/VBlank state and which interrupt types are enabled.
/// 
/// Partially writable.
/// </summary>
internal struct GeneralLcdStatus
{
    internal bool VBlankFlag;
    internal bool HBlankFlag;
    internal bool VCounterFlag;
    internal bool VBlankIrqEnable;
    internal bool HBlankIrqEnable;
    internal bool VCounterIrqEnable;
    internal ushort VCountSetting;

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
        VBlankIrqEnable = (value & 0b1000) == 0b1000;
        HBlankIrqEnable = (value & 0b1_0000) == 0b1_0000;
        VCounterIrqEnable = (value & 0b10_0000) == 0b10_0000;
        VCountSetting = (ushort)(value >> 8);
    }
}
