using GameboyAdvanced.Core.Dma;

namespace GameboyAdvanced.Core.Rom;

/// <summary>
/// Games like Donkey Kong used a form of persistent storage called EEProm.
/// 
/// That storage can be accessed on the address/data line to the ROM chip
/// and requires a different state machine to the similar flash storage.
/// 
/// This class encapsulates that state machine and handles read/writes from
/// EEProm.
/// </summary>
public class EEPromBackup
{
    public enum EEPromSize
    {
        Unknown,
        Small4Kb,
        Large64Kb,
    }

    public enum EEPromState
    {
        Waiting,
        WaitingSecondBit,
        ReceivingAddress,
        ReadingValue,
        WritingValue,
        WaitingNullTerminator,
    }

    public enum EEPromCommand
    {
        Waiting,
        Read,
        Write,
    }

    public readonly byte[] Data = new byte[0x2000];
    public EEPromSize Size;
    public EEPromState State;
    public EEPromCommand Command;
    public readonly uint _eepromMask;
    public int _maxAddressBits;
    public int _addressBitsReceived;
    public int _dataBitsReceived;
    public int _addressBlock;
    public int _address;

    public DmaDataUnit? DmaDataUnit;

    public EEPromBackup(uint eepromMask, EEPromSize size = EEPromSize.Unknown)
    {
        DmaDataUnit = null;
        _eepromMask = eepromMask;
        SetSize(size);

        Array.Fill<byte>(Data, 0xFF);
    }

    internal void SetSize(EEPromSize size)
    {
        Size = size;
        _maxAddressBits = Size switch
        {
            EEPromSize.Unknown => 1,
            EEPromSize.Small4Kb => 6,
            EEPromSize.Large64Kb => 14,
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, $"Size set to invalid value {size}")
        };
    }

    internal void Write(uint address, byte value)
    {
        
        if (Size == EEPromSize.Unknown)
        {
            SetSize(DmaDataUnit!.Channels[3].WordCount switch
            {
                9 => EEPromSize.Small4Kb,
                17 => EEPromSize.Large64Kb,
                9 + 64 => EEPromSize.Small4Kb,
                17 + 64 => EEPromSize.Large64Kb,
                _ => EEPromSize.Small4Kb, // Guess small if we're really not sure
            });
        }

        switch (State)
        {
            case EEPromState.Waiting:
                if ((value & 0b1) == 0b1)
                {
                    State = EEPromState.WaitingSecondBit;
                }
                break;
            case EEPromState.WaitingSecondBit:
                Command = (value & 0b1) == 0b1 ? EEPromCommand.Read : EEPromCommand.Write;
                State = EEPromState.ReceivingAddress;
                _addressBitsReceived = 0;
                _addressBlock = 0;
                _address = 0;
                break;
            case EEPromState.ReceivingAddress:
                _addressBlock = (_addressBlock << 1) | (value & 0b1);
                _addressBitsReceived++;

                if (_maxAddressBits == _addressBitsReceived)
                {
                    // Addressing is done in 64 bit blocks for both read and
                    // write. For large EEPROM the address is 14 bits wide but
                    // only the 10 LSB are relevant
                    _address = (_addressBlock & 0b11_1111_1111) * 8;
                    _dataBitsReceived = Command == EEPromCommand.Read ? -4 : 0;
                    State = Command switch
                    {
                        EEPromCommand.Write => EEPromState.WritingValue,
                        _ => EEPromState.WaitingNullTerminator,
                    };
                }
                break;
            case EEPromState.WritingValue:
                var writeAddress = _address + (_dataBitsReceived / 8);
                var maskedValue = (value & 1) << (_dataBitsReceived % 8);
                if (_dataBitsReceived % 8 == 0)
                {
                    Data[writeAddress] = (byte)maskedValue;
                }
                else
                {
                    Data[writeAddress] = (byte)((Data[writeAddress] & ~maskedValue) | maskedValue);
                }

                _dataBitsReceived++;

                if (_dataBitsReceived == 64)
                {
                    State = EEPromState.WaitingNullTerminator;
                }
                break;
            case EEPromState.WaitingNullTerminator:
                if ((value & 0b1) == 0b0)
                {
                    State = Command == EEPromCommand.Read ? EEPromState.ReadingValue : EEPromState.Waiting;
                }
                break;
        }
    }

    internal byte Read(uint _)
    {
        if (State is EEPromState.ReadingValue or EEPromState.WaitingNullTerminator)
        {
            if (_dataBitsReceived >= 0)
            {
                var readAddress = _address + (_dataBitsReceived / 8);
                var data = (Data[readAddress] >> (_dataBitsReceived % 8)) & 0b1;
                _dataBitsReceived++;

                if (_dataBitsReceived == 64)
                {
                    State = EEPromState.Waiting;
                }
                return (byte)data;
            }
            else
            {
                _dataBitsReceived++;
                return 0xFF; // First 4 bits are ignored, just return 0xFF until we know better
            }
            
        }

        return 0xFF; // TODO - What gets read from EEProm when in the wrong state?
    }

    internal bool IsEEPromAddress(uint address) => (address & _eepromMask) == _eepromMask;
}
