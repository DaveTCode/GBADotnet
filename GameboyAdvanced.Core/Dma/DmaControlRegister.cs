namespace GameboyAdvanced.Core.Dma;

internal enum DestAddressCtrl
{
    Increment = 0b00,
    Decrement = 0b01,
    Fixed = 0b10,
    IncrememntReload = 0b11,
}

internal enum SrcAddressCtrl
{
    Increment = 0b00,
    Decrement = 0b01,
    Fixed = 0b10,
    Prohibited = 0b11,
}

internal enum StartTiming
{
    Immediate = 0b00,
    VBlank = 0b01,
    HBlank = 0b10,
    Special = 0b11,
}

internal struct DmaControlRegister
{
    internal DestAddressCtrl DestAddressCtrl;
    internal SrcAddressCtrl SrcAddressCtrl;
    internal bool Repeat;
    internal bool Is32Bit;
    internal bool GamePakDRQ;
    internal StartTiming StartTiming;
    internal bool IrqOnEnd;
    internal bool DmaEnable;

    internal bool Update(uint val)
    {
        DestAddressCtrl = (DestAddressCtrl)((val >> 5) & 0b11);
        SrcAddressCtrl = (SrcAddressCtrl)((val >> 7) & 0b11);
        Repeat = ((val >> 9) & 0b1) == 0b1;
        Is32Bit = ((val >> 10) & 0b1) == 0b1;
        GamePakDRQ = ((val >> 11) & 0b1) == 0b1;
        StartTiming = (StartTiming)((val >> 12) & 0b11);
        IrqOnEnd = ((val >> 14) & 0b1) == 0b1;

        var oldEnable = DmaEnable;
        DmaEnable = ((val >> 15) & 0b1) == 0b1;

        return !oldEnable && DmaEnable;
    }

    internal ushort Read() => (ushort)
        (((uint)DestAddressCtrl << 5) |
         ((uint)SrcAddressCtrl << 7) |
         (Repeat ? (1u << 9) : 0) |
         (Is32Bit ? (1u << 10) : 0) |
         (GamePakDRQ ? (1u << 11) : 0) |
         ((uint)StartTiming << 12) |
         (IrqOnEnd ? (1u << 14) : 0) |
         (DmaEnable ? (1u << 15) : 0));
}
