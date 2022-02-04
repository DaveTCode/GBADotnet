namespace GameboyAdvanced.Core.Dma;

internal struct DmaChannel
{
    private static int[] MaxWordCounts = new int[] { 0x4000, 0x4000, 0x4000, 0x10000 };

    internal int Id;
    internal uint SourceAddress;
    internal uint DestinationAddress;
    internal ushort WordCount;

    internal uint IntSourceAddress;
    internal uint IntDestinationAddress;
    internal int IntWordCount;

    internal DmaControlRegister ControlReg;

    private int _clocksToStart;

    internal DmaChannel(int id)
    {
        Id = id;
        SourceAddress = IntSourceAddress = 0;
        DestinationAddress = IntDestinationAddress = 0;
        WordCount = 0;
        IntWordCount = 0;
        ControlReg = new DmaControlRegister();
        _clocksToStart = 0;
    }

    internal void UpdateControlRegister(ushort value)
    {
        var enableBitFlip = ControlReg.Update(value);

        if (enableBitFlip)
        {
            _clocksToStart = 2; // 2 clock cycles after setting before DMA starts
            IntSourceAddress = SourceAddress;
            IntDestinationAddress = DestinationAddress;
            IntWordCount = (WordCount == 0) ? MaxWordCounts[Id] : WordCount; // 0 is a special case that means copy MAX bytes
        }
    }

    public override string ToString() => $"DMA Channel {Id}";
}
