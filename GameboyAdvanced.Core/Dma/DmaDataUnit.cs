using static GameboyAdvanced.Core.IORegs;

namespace GameboyAdvanced.Core.Dma;

/// <summary>
/// The DMA component is split into two parts:
/// 1. The data unit (this) which contains data on the channels and handles register read/writes
/// 2. The controller which steps the unit and holds internal state (e.g. waitstates)
/// 
/// This is done because the Memory bus needs access to the data unit for writes but the
/// controller also needs access back to the bus for read/write during a DMA.
/// </summary>
internal class DmaDataUnit
{
    internal readonly DmaChannel[] Channels = new DmaChannel[4]
    {
        new DmaChannel(0), new DmaChannel(1), new DmaChannel(2), new DmaChannel(3)
    };

    internal void Reset()
    {
        foreach (var channel in Channels)
        {
            channel.Reset();
        }
    }

    internal byte ReadByte(uint address) => address switch
    {
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is not mapped for DMA read byte") // TODO - Handle unused addresses properly
    };

    internal ushort ReadHalfWord(uint address, uint openbus) => address switch
    {
        DMA0CNT_L => 0,
        DMA0CNT_H => Channels[0].ControlReg.Read(),
        DMA1CNT_L => 0,
        DMA1CNT_H => Channels[1].ControlReg.Read(),
        DMA2CNT_L => 0,
        DMA2CNT_H => Channels[2].ControlReg.Read(),
        DMA3CNT_L => 0,
        DMA3CNT_H => Channels[3].ControlReg.Read(),
        _ => (ushort)openbus,
    };

    internal uint ReadWord(uint address, uint openbus) => ReadHalfWord(address, openbus);

    internal void WriteByte(uint address, byte value)
    {
        throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is not mapped for DMA write"); // TODO - Handle unused addresses properly
    }

    internal void WriteHalfWord(uint address, ushort value)
    {
        switch (address)
        {
            case DMA0CNT_L:
                Channels[0].WordCount = (ushort)(value & 0x3FFF);
                break;
            case DMA0CNT_H:
                Channels[0].UpdateControlRegister(value);
                break;
            case DMA1CNT_L:
                Channels[1].WordCount = (ushort)(value & 0x3FFF);
                break;
            case DMA1CNT_H:
                Channels[1].UpdateControlRegister(value);
                break;
            case DMA2CNT_L:
                Channels[2].WordCount = (ushort)(value & 0x3FFF);
                break;
            case DMA2CNT_H:
                Channels[2].UpdateControlRegister(value);
                break;
            case DMA3CNT_L:
                Channels[3].WordCount = value; // Accepts lengths up to 0xFFFF so no mask
                break;
            case DMA3CNT_H:
                Channels[3].UpdateControlRegister(value);
                break;
            default:
                break;
        }
    }

    internal void WriteWord(uint address, uint value)
    {
        switch (address)
        {
            case DMA0SAD:
                Channels[0].SourceAddress = value & 0x07FF_FFFF;
                break;
            case DMA0DAD:
                Channels[0].DestinationAddress = value & 0x07FF_FFFF;
                break;
            case DMA0CNT_L:
                Channels[0].WordCount = (ushort)(value & 0x3FFF);
                Channels[0].UpdateControlRegister((ushort)(value >> 16));
                break;
            case DMA1SAD:
                Channels[1].SourceAddress = value & 0x0FFF_FFFF;
                break;
            case DMA1DAD:
                Channels[1].DestinationAddress = value & 0x07FF_FFFF;
                break;
            case DMA1CNT_L:
                Channels[1].WordCount = (ushort)(value & 0x3FFF);
                Channels[1].UpdateControlRegister((ushort)(value >> 16));
                break;
            case DMA2SAD:
                Channels[2].SourceAddress = value & 0x0FFF_FFFF;
                break;
            case DMA2DAD:
                Channels[2].DestinationAddress = value & 0x07FF_FFFF;
                break;
            case DMA2CNT_L:
                Channels[2].WordCount = (ushort)(value & 0x3FFF);
                Channels[2].UpdateControlRegister((ushort)(value >> 16));
                break;
            case DMA3SAD:
                Channels[3].SourceAddress = value & 0x0FFF_FFFF;
                break;
            case DMA3DAD:
                Channels[3].DestinationAddress = value & 0x0FFF_FFFF;
                break;
            case DMA3CNT_L:
                Channels[3].WordCount = (ushort)value;
                Channels[3].UpdateControlRegister((ushort)(value >> 16));
                break;
            default:
                break;
        }
    }
}
