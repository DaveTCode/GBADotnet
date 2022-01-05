namespace GameboyAdvanced.Core;

internal enum CPSRMode
{
    OldUser = 0x0,
    OldFiq = 0x1,
    OldIrq = 0x2,
    OldSupervisor = 0x3,
    User = 0x10,
    Fiq = 0x11,
    Irq = 0x12,
    Supervisor = 0x13,
    Abort = 0x17,
    Undefined = 0x1B,
    System = 0x1F,
}

internal struct CPSR
{
    internal bool SignFlag;
    internal bool ZeroFlag;
    internal bool CarryFlag;
    internal bool OverflowFlag;
    internal bool StickyOverflow;
    internal bool JazelleMode;
    internal bool BigEndian;
    internal bool AbortDisable;
    internal bool IrqDisable;
    internal bool FiqDisable;
    internal bool ThumbMode;
    internal CPSRMode Mode;

    public CPSR()
    {
        SignFlag = false;
        ZeroFlag = false;
        CarryFlag = false;
        OverflowFlag = false;
        StickyOverflow = false;
        JazelleMode = false;
        BigEndian = false;
        AbortDisable = false;
        IrqDisable = false;
        FiqDisable = false;
        ThumbMode = false;
        Mode = CPSRMode.System; // TODO - What's the initial mode?
    }

    internal uint Get() => (SignFlag ? 0x8000_0000 : 0)
             | (uint)(ZeroFlag ? 0x4000_0000 : 0)
             | (uint)(CarryFlag ? 0x2000_0000 : 0)
             | (uint)(OverflowFlag ? 0x1000_0000 : 0)
             | (uint)(StickyOverflow ? 0x0800_0000 : 0)
             | (uint)(JazelleMode ? 0x0100_0000 : 0)
             | (uint)(BigEndian ? 0x200 : 0)
             | (uint)(AbortDisable ? 0x100 : 0)
             | (uint)(IrqDisable ? 0x80 : 0)
             | (uint)(FiqDisable ? 0x40 : 0)
             | (uint)(ThumbMode ? 0x20 : 0);
}
