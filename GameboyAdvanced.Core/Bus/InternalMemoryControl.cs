namespace GameboyAdvanced.Core.Bus;

/// <summary>
/// This struct represents the undocumented register mirrored across 0x04xx0800
/// which contains the wait states for WRAM
/// </summary>
public struct InternalMemoryControl
{
    private const uint Mask = 0b1111_1111_0000_0000_0000_0000_0010_1111;
    private uint _raw;

    internal bool IsWRAMDisabled;
    internal bool Is256KBWRAM;
    internal int WaitControlWRAM;

    public InternalMemoryControl()
    {
        _raw = 0x0D00_0020;
        IsWRAMDisabled = false;
        Is256KBWRAM = true;
        WaitControlWRAM = 2;
        Update();
    }
    
    internal void Reset()
    {
        _raw = 0x0D00_0020;
        Update();
    }

    internal uint Get()
    {
        return _raw;
    }

    internal void Set(uint val)
    {
        _raw = val & Mask;
        Update();
    }

    private void Update()
    {
        IsWRAMDisabled = (_raw & 0b1) == 0b1;
        Is256KBWRAM = ((_raw >> 5) & 0b1) == 0b1;
        WaitControlWRAM = (int)(15 - ((_raw >> 24) & 0b1111)); // TODO - Not handling case when val == 15 which implies lockup
    }
}
