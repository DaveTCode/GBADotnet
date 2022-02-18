namespace GameboyAdvanced.Core.Dma;

internal class DmaController
{
    private readonly DmaChannel[] _channels = new DmaChannel[4]
    {
        new DmaChannel(0), new DmaChannel(1), new DmaChannel(2), new DmaChannel(3)
    };

    internal void Reset()
    {
        for (var ii = 0; ii < _channels.Length; ii++)
        {
            _channels[ii] = new DmaChannel(ii);
        }
    }

    internal void Step()
    {
        // TODO - Actually implement DMA
    }

    #region Memory Read Write

    internal byte ReadByte(uint address) => address switch
    {
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is not mapped for DMA read byte") // TODO - Handle unused addresses properly
    };

    internal ushort ReadHalfWord(uint address) => address switch
    {
        0x0400_00BA => _channels[0].ControlReg.Read(),
        0x0400_00C6 => _channels[1].ControlReg.Read(),
        0x0400_00D2 => _channels[2].ControlReg.Read(),
        0x0400_00DE => _channels[3].ControlReg.Read(),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is not mapped for DMA read half word") // TODO - Handle unused addresses properly
    };

    internal uint ReadWord(uint address) => throw new NotImplementedException("Word reads from DMA registers not implemented");

    internal void WriteByte(uint address, byte value)
    {
        throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is not mapped for DMA write"); // TODO - Handle unused addresses properly
    }

    internal void WriteHalfWord(uint address, ushort value)
    {
        switch (address)
        {
            case 0x0400_00B8: // DMA0CNT_L
                _channels[0].WordCount = (ushort)(value & 0x3FFF);
                break;
            case 0x0400_00BA: // DMA0CNT_H
                _channels[0].UpdateControlRegister(value);
                break;
            case 0x0400_00C4: // DMA1CNT_L
                _channels[1].WordCount = (ushort)(value & 0x3FFF);
                break;
            case 0x0400_00C6: // DMA0CNT_H
                _channels[1].UpdateControlRegister(value);
                break;
            case 0x0400_00D0: // DMA2CNT_L
                _channels[2].WordCount = (ushort)(value & 0x3FFF);
                break;
            case 0x0400_00D2: // DMA0CNT_H
                _channels[2].UpdateControlRegister(value);
                break;
            case 0x0400_00DC: // DMA3CNT_L
                _channels[3].WordCount = value; // Accepts lengths up to 0xFFFF so no mask
                break;
            case 0x0400_00DE: // DMA0CNT_H
                _channels[3].UpdateControlRegister(value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is not mapped for DMA half word write"); // TODO - Handle unused addresses properly
        }
    }

    internal void WriteWord(uint address, uint value)
    {
        switch (address)
        {
            case 0x0400_00B0: // DMA0SAD
                _channels[0].SourceAddress = value & 0x0FFF_FFFF; // TODO - when is this mask 0x07FF_FFFF instead of 0x0FFF_FFFF?
                break;
            case 0x0400_00B4: // DMA0DAD
                _channels[0].DestinationAddress = value & 0x0FFF_FFFF; // TODO - when is this mask 0x07FF_FFFF instead of 0x0FFF_FFFF?
                break;
            case 0x0400_00B8: // DMA0CNT
                _ = _channels[0].ControlReg.Update(value); // TODO - Presumably this might trigger a DMA
                break;
            case 0x0400_00BC: // DMA1SAD
                _channels[1].SourceAddress = value & 0x0FFF_FFFF;
                break;
            case 0x0400_00C0: // DMA1DAD
                _channels[1].DestinationAddress = value & 0x0FFF_FFFF; // TODO - when is this mask 0x07FF_FFFF instead of 0x0FFF_FFFF?
                break;
            case 0x0400_00C4: // DMA1CNT
                _ = _channels[1].ControlReg.Update(value); // TODO - Presumably this might trigger a DMA
                break;
            case 0x0400_00C8: // DMA2SAD
                _channels[2].SourceAddress = value & 0x0FFF_FFFF;
                break;
            case 0x0400_00CC: // DMA2DAD
                _channels[2].DestinationAddress = value & 0x0FFF_FFFF; // TODO - when is this mask 0x07FF_FFFF instead of 0x0FFF_FFFF?
                break;
            case 0x0400_00D0: // DMA2CNT
                _ = _channels[2].ControlReg.Update(value); // TODO - Presumably this might trigger a DMA
                break;
            case 0x0400_00D4: // DMA3SAD
                _channels[3].SourceAddress = value & 0x0FFF_FFFF;
                break;
            case 0x0400_00D8: // DMA3DAD
                _channels[3].DestinationAddress = value & 0x0FFF_FFFF; // TODO - when is this mask 0x07FF_FFFF instead of 0x0FFF_FFFF?
                break;
            case 0x0400_00DC: // DMA3CNT
                _ = _channels[3].ControlReg.Update(value); // TODO - Presumably this might trigger a DMA
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is not mapped for DMA word write"); // TODO - Handle unused addresses properly
        }
    }

    #endregion
}
