namespace GameboyAdvanced.Core.Apu.Channels;

public class DmaChannel : BaseChannel
{
    /// <summary>
    /// This is the value that's most recently been popped off the sample 
    /// buffer and is going to be used by the main audio systems.
    /// 
    /// It has already had volume applied to it from the channel settings
    /// but not global audio settings.
    /// </summary>
    public short CurrentValue;

    public byte[] Fifo = new byte[32];
    public int FifoReadPtr;
    public int FifoWritePtr;
    public bool FullVolume;
    public bool EnableRight;
    public bool EnableLeft;
    public bool SelectTimer1;

    private readonly Device _device;
    private readonly uint _channelAddress;

    public DmaChannel(int index, Device device) : base(index)
    {
        _device = device;
        _channelAddress = index == 1 ? IORegs.FIFO_A : IORegs.FIFO_B;
    }

    internal override void Reset()
    {
        FullVolume = false;
        EnableRight = false;
        EnableLeft = false;
        SelectTimer1 = false;
        ResetFifo();
    }

    internal void StepFifo()
    {
        // First check if the FIFO buffer is empty, if it is then use a sample
        // of 0
        if (FifoReadPtr == FifoWritePtr)
        {
            CurrentValue = 0;
        }
        else
        {
            CurrentValue = (short)((sbyte)Fifo[FifoReadPtr] << (FullVolume ? 1 : 0));
            FifoReadPtr++;
        }

        // If the FIFO buffer doesn't have enough data left then request a DMA
        // from whichever DMA channel is controlling this audio channel
        if (FifoWritePtr - FifoReadPtr <= 4)
        {
            for (var ii = 1; ii < 3; ii++)
            {
                var dmaChannel = _device.DmaData.Channels[ii];
                if (dmaChannel.ControlReg.DmaEnable && 
                    dmaChannel.ControlReg.StartTiming == Dma.StartTiming.Special &&
                    dmaChannel.DestinationAddress == _channelAddress)
                {
                    dmaChannel.IsRunning = true;
                    dmaChannel.ClocksToStart = 3;
                    dmaChannel.IntSrcSeqAccess = 0;
                    dmaChannel.IntDestSeqAccess = 0;
                }
            }
            
        }
    }

    internal void ResetFifo()
    {
        Array.Clear(Fifo);
        FifoReadPtr = 0;
        FifoWritePtr = 0;
    }

    internal void InsertSampleByte(byte value)
    {
        if (FifoWritePtr == Fifo.Length) return; // TODO - Right behaviour? Just ignore bytes when full? Or overwrite in circle?

        Fifo[FifoWritePtr] = value;
        FifoWritePtr++;

        if (FifoWritePtr == Fifo.Length)
        {
            for (var ii = FifoReadPtr; ii < FifoWritePtr; ii++)
            {
                Fifo[ii - FifoReadPtr] = Fifo[ii];
            }

            FifoWritePtr -= FifoReadPtr;
            FifoReadPtr = 0;
        }
    }
}
