namespace GameboyAdvanced.Core.Dma;

public struct DmaChannel
{
    internal readonly static int[] MaxWordCounts = new int[] { 0x4000, 0x4000, 0x4000, 0x10000 };

    internal int Id;
    internal uint SourceAddress;
    internal uint DestinationAddress;
    internal ushort WordCount;

    internal uint IntSourceAddress;
    internal uint IntDestinationAddress;
    internal uint? IntCachedValue;
    internal int IntWordCount;
    internal int IntDestAddressIncrement;
    internal int IntSrcAddressIncrement;
    internal uint InternalLatch;
    internal int IntDestSeqAccess;
    internal int IntSrcSeqAccess;

    internal DmaControlRegister ControlReg;

    internal int ClocksToStart;

    internal DmaChannel(int id)
    {
        Id = id;
        SourceAddress = IntSourceAddress = 0;
        DestinationAddress = IntDestinationAddress = 0;
        WordCount = 0;
        IntCachedValue = null;
        IntWordCount = 0;
        IntDestAddressIncrement = 0;
        IntDestSeqAccess = 0;
        IntSrcAddressIncrement = 0;
        IntSrcSeqAccess = 0;
        ControlReg = new DmaControlRegister(id);
        ClocksToStart = 0;
        InternalLatch = 0;
    }

    internal void UpdateControlRegister(ushort value)
    {
        var enableBitFlip = ControlReg.Update(value);

        if (enableBitFlip)
        {
            ClocksToStart = 3; // 2I cycles after setting register before DMA unit starts processing and THEN 1 cycle before write start (and one at the end)
            IntSrcSeqAccess = 0; // 1st read/write pair are non-sequential
            IntDestSeqAccess = 0; // 1st read/write pair are non-sequential

            // Both source and destination addresses are forcibly aligned to halfword/word boundaries
            IntSourceAddress = SourceAddress & (ControlReg.Is32Bit ? 0xFFFF_FFFC : 0xFFFF_FFFE);
            IntDestinationAddress = DestinationAddress & (ControlReg.Is32Bit ? 0xFFFF_FFFC : 0xFFFF_FFFE);
            IntCachedValue = null;
            IntWordCount = (WordCount == 0) ? MaxWordCounts[Id] : WordCount; // 0 is a special case that means copy MAX bytes
            IntDestAddressIncrement = (ControlReg.Is32Bit, ControlReg.DestAddressCtrl) switch
            {
                (_, DestAddressCtrl.Fixed) => 0,
                (true, DestAddressCtrl.Increment) => 4,
                (true, DestAddressCtrl.IncrementReload) => 4,
                (true, DestAddressCtrl.Decrement) => -4,
                (false, DestAddressCtrl.Increment) => 2,
                (false, DestAddressCtrl.IncrementReload) => 2,
                (false, DestAddressCtrl.Decrement) => -2,
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
        ControlReg.Reset();
    }

    public override string ToString() => $"DMA Channel {Id}";
}
