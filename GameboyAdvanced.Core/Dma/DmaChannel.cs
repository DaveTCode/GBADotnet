using GameboyAdvanced.Core.Interrupts;

namespace GameboyAdvanced.Core.Dma;

public class DmaChannel
{
    internal readonly static int[] MaxWordCounts = new int[] { 0x4000, 0x4000, 0x4000, 0x10000 };

    public int Id;
    public readonly ushort _wordMask;
    public readonly uint _srcMask;
    public readonly uint _destMask;
    public uint SourceAddress;
    public uint DestinationAddress;
    public ushort WordCount;

    public uint IntSourceAddress;
    public uint IntDestinationAddress;
    public uint? IntCachedValue;
    public int IntWordCount;
    public int IntDestAddressIncrement;
    public int IntSrcAddressIncrement;
    public uint InternalLatch;
    public int IntDestSeqAccess;
    public int IntSrcSeqAccess;

    public bool IsRunning;

    public DmaControlRegister ControlReg;

    public int ClocksToStart;
    public int ClocksToStop;

    internal DmaChannel(int id, ushort wordMask, uint srcMask, uint destMask)
    {
        Id = id;
        _wordMask = wordMask;
        _srcMask = srcMask;
        _destMask = destMask;
        ControlReg = new DmaControlRegister(Id);
        Reset();
    }

    internal void UpdateSourceAddress(uint byteIndex, byte value)
    {
        switch (byteIndex)
        {
            case 0: 
                SourceAddress = (SourceAddress & 0xFFFF_FF00) | value;
                break;
            case 1:
                SourceAddress = (SourceAddress & 0xFFFF_00FF) | (uint)(value << 8);
                break;
            case 2:
                SourceAddress = (SourceAddress & 0xFF00_FFFF) | (uint)(value << 16);
                break;
            case 3:
                SourceAddress = (SourceAddress & 0x00FF_FFFF) | (uint)(value << 24);
                break;
        }

        SourceAddress &= _srcMask;
    }

    internal void UpdateDestinationAddress(uint byteIndex, byte value)
    {
        switch (byteIndex)
        {
            case 0:
                DestinationAddress = (DestinationAddress & 0xFFFF_FF00) | value;
                break;
            case 1:
                DestinationAddress = (DestinationAddress & 0xFFFF_00FF) | (uint)(value << 8);
                break;
            case 2:
                DestinationAddress = (DestinationAddress & 0xFF00_FFFF) | (uint)(value << 16);
                break;
            case 3:
                DestinationAddress = (DestinationAddress & 0x00FF_FFFF) | (uint)(value << 24);
                break;
        }

        DestinationAddress &= _destMask;
    }

    internal void UpdateWordCount(uint byteIndex, byte value)
    {
        WordCount = byteIndex == 0 
            ? (ushort)((WordCount & 0xFF00) | value) 
            : (ushort)((WordCount & 0x00FF) | (ushort)(value << 8));

        WordCount &= _wordMask;
    }

    internal void UpdateControlRegister(byte value)
    {
        var enableBitFlip = ControlReg.UpdateB2(value);

        if (enableBitFlip)
        {
            if (ControlReg.StartTiming == StartTiming.Immediate)
            {
                // Changing a channel _to_ immediate isn't enough to start it
                // running. An immediate channel only starts running when the
                // enable bit flips
                IsRunning = true;
            }

            ClocksToStart = 3; // 2I cycles after setting register before DMA unit starts processing and THEN 1 cycle before read start (and one at the end)
            IntSrcSeqAccess = 0; // 1st read/write pair are non-sequential
            IntDestSeqAccess = 0; // 1st read/write pair are non-sequential

            // Both source and destination addresses are forcibly aligned to halfword/word boundaries
            IntSourceAddress = SourceAddress & (ControlReg.Is32Bit ? 0xFFFF_FFFC : 0xFFFF_FFFE);
            IntDestinationAddress = DestinationAddress & (ControlReg.Is32Bit ? 0xFFFF_FFFC : 0xFFFF_FFFE);
            IntCachedValue = null;
            if (Id is 1 or 2 && ControlReg.StartTiming == StartTiming.Special)
            {
                IntWordCount = 4;
            }
            else
            {
                IntWordCount = (WordCount == 0) ? MaxWordCounts[Id] : WordCount; // 0 is a special case that means copy MAX bytes
            }
            IntDestAddressIncrement = (ControlReg.Is32Bit, ControlReg.DestAddressCtrl, ControlReg.StartTiming) switch
            {
                (_, _, StartTiming.Special) => 0,
                (_, DestAddressCtrl.Fixed, _) => 0,
                (true, DestAddressCtrl.Increment, _) => 4,
                (true, DestAddressCtrl.IncrementReload, _) => 4,
                (true, DestAddressCtrl.Decrement, _) => -4,
                (false, DestAddressCtrl.Increment, _) => 2,
                (false, DestAddressCtrl.IncrementReload, _) => 2,
                (false, DestAddressCtrl.Decrement, _) => -2,
                _ => throw new Exception("Invalid destination address control")
            };
            IntSrcAddressIncrement = (ControlReg.Is32Bit, ControlReg.SrcAddressCtrl) switch
            {
                (_, SrcAddressCtrl.Fixed) => 0,
                (true, SrcAddressCtrl.Increment) => 4,
                (true, SrcAddressCtrl.Decrement) => -4,
                (false, SrcAddressCtrl.Increment) => 2,
                (false, SrcAddressCtrl.Decrement) => -2,
                (_, SrcAddressCtrl.Prohibited) => 0,
                _ => throw new Exception("Invalid source address control")
            };
        }
    }

    internal void StopChannel(InterruptInterconnect interruptInterconnect)
    {
        IntCachedValue = null;
        IsRunning = false;

        if (ControlReg.Repeat && ControlReg.StartTiming != StartTiming.Immediate)
        {
            if (Id is 1 or 2 && ControlReg.StartTiming == StartTiming.Special)
            {
                IntWordCount = 4;
            }
            else
            {
                IntWordCount = (WordCount == 0) ? MaxWordCounts[Id] : WordCount; // 0 is a special case that means copy MAX bytes
            }

            if (ControlReg.DestAddressCtrl == DestAddressCtrl.IncrementReload)
            {
                IntDestinationAddress = DestinationAddress & (ControlReg.Is32Bit ? 0xFFFF_FFFC : 0xFFFF_FFFE);
            }
        }
        else
        {
            ControlReg.DmaEnable = false;
        }

        if (ControlReg.IrqOnEnd)
        {
            interruptInterconnect.RaiseInterrupt(Id switch
            {
                0 => Interrupt.DMA0,
                1 => Interrupt.DMA1,
                2 => Interrupt.DMA2,
                3 => Interrupt.DMA3,
                _ => throw new Exception($"Invalid state, no DMA channel with index {Id}")
            });
        }
    }

    internal void Reset()
    {
        SourceAddress = 0;
        DestinationAddress = 0;
        WordCount = 0;
        IntSourceAddress = 0;
        IntDestinationAddress = 0;
        IntCachedValue = null;
        IntWordCount = 0;
        IntDestAddressIncrement = 0;
        IntSrcAddressIncrement = 0;
        ClocksToStart = 0;
        ClocksToStop = 0;
        ControlReg.Reset();
    }

    public override string ToString() => $"DMA{Id} {SourceAddress:X8}->{DestinationAddress:X8} ({WordCount}) - {ControlReg}";
}
