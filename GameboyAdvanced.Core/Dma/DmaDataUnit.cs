using GameboyAdvanced.Core.Apu.Channels;
using GameboyAdvanced.Core.Interrupts;
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
public class DmaDataUnit
{
    internal readonly DmaChannel[] Channels = new DmaChannel[4]
    {
        new DmaChannel(0, 0x3FFF, 0x07FF_FFFF, 0x07FF_FFFF), 
        new DmaChannel(1, 0x3FFF, 0x0FFF_FFFF, 0x07FF_FFFF), 
        new DmaChannel(2, 0x3FFF, 0x0FFF_FFFF, 0x07FF_FFFF), 
        new DmaChannel(3, 0xFFFF, 0x0FFF_FFFF, 0x0FFF_FFFF)
    };

    internal void Reset()
    {
        foreach (var channel in Channels)
        {
            channel.Reset();
        }
    }

    /// <summary>
    /// Triggered by the PPU when it hits the exact cycle where HDMA is started
    /// </summary>
    internal void StartHdma(int line)
    {
        for (var ii = 0; ii < Channels.Length; ii++)
        {
            if (Channels[ii].ControlReg.DmaEnable)
            {
                switch (Channels[ii].ControlReg.StartTiming)
                {
                    case StartTiming.HBlank:
                        if (line < Device.HEIGHT)
                        {
                            Channels[ii].IsRunning = true;
                        }
                        break;
                    case StartTiming.Special:
                        // Handle Video Capture Mode DMA
                        if (ii == 3)
                        {
                            if (line is >= 2 and < (Device.HEIGHT + 2) && ii == 3)
                            {
                                Channels[ii].IsRunning = true;
                            }
                            else if (line == Device.HEIGHT + 2)
                            {
                                Channels[ii].IntCachedValue = null;
                                Channels[ii].ControlReg.DmaEnable = false;
                            }
                        }
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Triggered by the PPU when it hits the exact cycle where VDMA is started
    /// </summary>
    internal void StartVdma()
    {
        for (var ii = 0; ii < Channels.Length; ii++)
        {
            if (Channels[ii].ControlReg.StartTiming == StartTiming.VBlank)
            {
                Channels[ii].IsRunning = true;
            }
        }
    }

    internal byte ReadByte(uint address, uint openbus) => (byte)(ReadHalfWord(address & 0xFFFF_FFFE, openbus) >> (int)(8 * (address & 0b1)));

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

    internal uint ReadWord(uint address, uint openbus) => 
        (uint)(ReadHalfWord(address, openbus) | (ReadHalfWord(address + 2, openbus) << 16));

    internal void WriteByte(uint address, byte value)
    {
        switch (address)
        {
            case DMA0SAD:
            case DMA0SAD + 1:
            case DMA0SAD + 2:
            case DMA0SAD + 3:
                Channels[0].UpdateSourceAddress(address & 0b11, value);
                break;
            case DMA0DAD:
            case DMA0DAD + 1:
            case DMA0DAD + 2:
            case DMA0DAD + 3:
                Channels[0].UpdateDestinationAddress(address & 0b11, value);
                break;
            case DMA1SAD:
            case DMA1SAD + 1:
            case DMA1SAD + 2:
            case DMA1SAD + 3:
                Channels[1].UpdateSourceAddress(address & 0b11, value);
                break;
            case DMA1DAD:
            case DMA1DAD + 1:
            case DMA1DAD + 2:
            case DMA1DAD + 3:
                Channels[1].UpdateDestinationAddress(address & 0b11, value);
                break;
            case DMA2SAD:
            case DMA2SAD + 1:
            case DMA2SAD + 2:
            case DMA2SAD + 3:
                Channels[2].UpdateSourceAddress(address & 0b11, value);
                break;
            case DMA2DAD:
            case DMA2DAD + 1:
            case DMA2DAD + 2:
            case DMA2DAD + 3:
                Channels[2].UpdateDestinationAddress(address & 0b11, value);
                break;
            case DMA3SAD:
            case DMA3SAD + 1:
            case DMA3SAD + 2:
            case DMA3SAD + 3:
                Channels[3].UpdateSourceAddress(address & 0b11, value);
                break;
            case DMA3DAD:
            case DMA3DAD + 1:
            case DMA3DAD + 2:
            case DMA3DAD + 3:
                Channels[3].UpdateDestinationAddress(address & 0b11, value);
                break;
            case DMA0CNT_L:
            case DMA0CNT_L + 1:
                Channels[0].UpdateWordCount(address & 0b1, value);
                break;
            case DMA1CNT_L:
            case DMA1CNT_L + 1:
                Channels[1].UpdateWordCount(address & 0b1, value);
                break;
            case DMA2CNT_L:
            case DMA2CNT_L + 1:
                Channels[2].UpdateWordCount(address & 0b1, value);
                break;
            case DMA3CNT_L:
            case DMA3CNT_L + 1:
                Channels[3].UpdateWordCount(address & 0b1, value);
                break;
            case DMA0CNT_H:
                Channels[0].ControlReg.UpdateB1(value);
                break;
            case DMA0CNT_H + 1:
                Channels[0].UpdateControlRegister(value);
                break;
            case DMA1CNT_H:
                Channels[1].ControlReg.UpdateB1(value);
                break;
            case DMA1CNT_H + 1:
                Channels[1].UpdateControlRegister(value);
                break;
            case DMA2CNT_H:
                Channels[2].ControlReg.UpdateB1(value);
                break;
            case DMA2CNT_H + 1:
                Channels[2].UpdateControlRegister(value);
                break;
            case DMA3CNT_H:
                Channels[3].ControlReg.UpdateB1(value);
                break;
            case DMA3CNT_H + 1:
                Channels[3].UpdateControlRegister(value);
                break;
        }
    }

    internal void WriteHalfWord(uint address, ushort value)
    {
        WriteByte(address, (byte)value);
        WriteByte(address + 1, (byte)(value >> 8));
    }

    internal void WriteWord(uint address, uint value)
    {
        WriteHalfWord(address, (ushort)value);
        WriteHalfWord(address + 2, (ushort)(value >> 16));
    }
}
