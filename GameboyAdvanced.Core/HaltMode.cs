namespace GameboyAdvanced.Core;

/// <summary>
/// By writing to HALTCNT (0x0400_0301) the GBA can be put into a low power 
/// mode where it will only wake up on interrupts.
/// 
/// Halt = CPU Paused
/// Stop = CPU/PPU/APU Paused
/// </summary>
public enum HaltMode
{
    Halt = 0b0,
    Stop = 0b1,
    None,
}
