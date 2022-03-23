﻿namespace GameboyAdvanced.Core.Dma;

internal enum DestAddressCtrl
{
    Increment = 0b00,
    Decrement = 0b01,
    Fixed = 0b10,
    IncrementReload = 0b11,
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

public struct DmaControlRegister
{
    private readonly int _dmaChannelId;
    internal DestAddressCtrl DestAddressCtrl;
    internal SrcAddressCtrl SrcAddressCtrl;
    internal bool Repeat;
    internal bool Is32Bit;
    internal bool GamePakDRQ;
    internal StartTiming StartTiming;
    internal bool IrqOnEnd;
    internal bool DmaEnable;

    internal DmaControlRegister(int id)
    {
        _dmaChannelId = id;
        DestAddressCtrl = DestAddressCtrl.Increment;
        SrcAddressCtrl = SrcAddressCtrl.Increment;
        Repeat = false;
        Is32Bit = false;
        GamePakDRQ = false;
        StartTiming = StartTiming.Immediate;
        IrqOnEnd = false;
        DmaEnable = false;
    }

    internal void UpdateB1(byte val)
    {
        DestAddressCtrl = (DestAddressCtrl)((val >> 5) & 0b11);
        SrcAddressCtrl = (SrcAddressCtrl)(((byte)SrcAddressCtrl) | (val >> 7));
    }

    internal bool UpdateB2(byte val)
    {
        SrcAddressCtrl = (SrcAddressCtrl)(((byte)SrcAddressCtrl) | (val & 0b1) << 1);
        Repeat = ((val >> 1) & 0b1) == 0b1;
        Is32Bit = ((val >> 2) & 0b1) == 0b1;
        GamePakDRQ = (_dmaChannelId == 3) && ((val >> 3) & 0b1) == 0b1;
        StartTiming = (StartTiming)((val >> 4) & 0b11);
        IrqOnEnd = ((val >> 6) & 0b1) == 0b1;

        var oldEnable = DmaEnable;
        DmaEnable = ((val >> 7) & 0b1) == 0b1;

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

    internal void Reset()
    {
        DestAddressCtrl = DestAddressCtrl.Increment;
        SrcAddressCtrl = SrcAddressCtrl.Increment;
        Repeat = false;
        Is32Bit = false;
        GamePakDRQ = false;
        StartTiming = StartTiming.Immediate;
        IrqOnEnd = false;
        DmaEnable = false;
    }
}
