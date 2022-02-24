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

internal static class CPSRModeExtensions
{
    internal static int Index(this CPSRMode mode) => mode switch
    {
        CPSRMode.OldUser => 0,
        CPSRMode.OldFiq => 1,
        CPSRMode.OldIrq => 4,
        CPSRMode.OldSupervisor => 2,
        CPSRMode.User => 0,
        CPSRMode.Fiq => 1,
        CPSRMode.Irq => 4,
        CPSRMode.Supervisor => 2,
        CPSRMode.Abort => 3,
        CPSRMode.Undefined => 5,
        CPSRMode.System => 0,
        _ => throw new Exception($"Invalid cpsr mode {mode}"),
    };
}

internal struct CPSR
{
    internal bool SignFlag;
    internal bool ZeroFlag;
    internal bool CarryFlag;
    internal bool OverflowFlag;
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
        AbortDisable = false;
        IrqDisable = false;
        FiqDisable = false;
        ThumbMode = false;
        Mode = CPSRMode.System;
    }

    internal uint Get() => (SignFlag ? 0x8000_0000 : 0)
             | (uint)(ZeroFlag ? 0x4000_0000 : 0)
             | (uint)(CarryFlag ? 0x2000_0000 : 0)
             | (uint)(OverflowFlag ? 0x1000_0000 : 0)
             | (uint)(AbortDisable ? 0x100 : 0)
             | (uint)(IrqDisable ? 0x80 : 0)
             | (uint)(FiqDisable ? 0x40 : 0)
             | (uint)(ThumbMode ? 0x20 : 0)
             | (uint)Mode;

    internal CPSRMode Set(uint v)
    {
        SignFlag = (v & 0x8000_0000) == 0x8000_0000;
        ZeroFlag = (v & 0x4000_0000) == 0x4000_0000;
        CarryFlag = (v & 0x2000_0000) == 0x2000_0000;
        OverflowFlag = (v & 0x1000_0000) == 0x1000_0000;
        AbortDisable = (v & 0x100) == 0x100;
        IrqDisable = (v & 0x80) == 0x80;
        FiqDisable = (v & 0x40) == 0x40;
        ThumbMode = (v & 0x20) == 0x20;

        
        return (CPSRMode)(v & 0b1_1111);
    }

    public override string ToString() => 
        Get().ToString("X8") + 
        " " +
        (SignFlag ? "N" : "-") +
        (ZeroFlag ? "Z" : "-") +
        (CarryFlag ? "C" : "-") +
        (OverflowFlag ? "V" : "-") +
        (AbortDisable ? "A" : "-") +
        (IrqDisable ? "I" : "-") +
        (FiqDisable ? "F" : "-") +
        (ThumbMode ? " Thm " : " Arm ") +
        Mode;
}
