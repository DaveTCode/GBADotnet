namespace GameboyAdvanced.Core.Rom;

/// <summary>
/// Some games used persistent backup memory with a flash chip on the gamepak 
/// (e.g. Pokemon Emerald).
/// 
/// This class encapsulates the state machine required to read/write data 
/// from that flash memory.
/// 
/// Note: Many edge cases around this state machine are not well understood at
/// all. e.g. What happens if a command is interrupted whilst being prepared?
/// These edge cases can't be easily tested because we can't flash the right 
/// sort of cartridge to test them on a genuine device.
/// </summary>
public class FlashBackup
{
    public enum FlashChipState
    {
        Ready,
        ChipIdentification,
        Erasing,
        Writing,
        SettingMemoryBank,
    }

    public enum FlashCommandState
    {
        NotStarted,
        Command1,
        Command2,
    }

    public enum FlashCommand
    {
        EraseChip = 0x10,
        Erase4KBSector = 0x30,
        PrepareReceiveErase = 0x80,
        EnterIdentificationMode = 0x90,
        PrepareWriteByte = 0xA0,
        SetMemoryBank = 0xB0,
        ExitIdentificationMode = 0xF0,
    }

    public readonly byte[] _data = new byte[2 * 0x1_0000];
    public readonly byte _manufacturerId;
    public readonly byte _deviceId;
    public int _bank;

    public FlashChipState _state;
    public FlashCommandState _commandState;

    public FlashBackup(byte manufacturerId, byte deviceId)
    {
        _manufacturerId = manufacturerId;
        _deviceId = deviceId;
        _bank = 0;
        _state = FlashChipState.Ready;
    }

    internal void Write(uint address, byte value)
    {
        switch (_commandState)
        {
            case FlashCommandState.NotStarted:
                switch (_state)
                {
                    case FlashChipState.Writing:
                        _data[(_bank * 0x1_0000) + (address & 0xFFFF)] = value;
                        _state = FlashChipState.Ready;
                        break;
                    case FlashChipState.SettingMemoryBank:
                        if (address == 0x0E00_0000)
                        {
                            _bank = value & 0b1;
                            _state = FlashChipState.Ready;
                        }
                        break;
                    default:
                        if (address == 0x0E00_5555 && value == 0xAA)
                        {
                            _commandState = FlashCommandState.Command1;
                        }
                        break;
                }
                break;
            case FlashCommandState.Command1:
                if (address == 0x0E00_2AAA && value == 0x55)
                {
                    _commandState = FlashCommandState.Command2;
                }
                break;
            case FlashCommandState.Command2:
                switch (_state)
                {
                    case FlashChipState.Ready:
                        if (address == 0x0E00_5555)
                        {
                            _state = value switch
                            {
                                0x80 => FlashChipState.Erasing,
                                0x90 => FlashChipState.ChipIdentification,
                                0xA0 => FlashChipState.Writing,
                                0xB0 => FlashChipState.SettingMemoryBank,
                                _ => _state,
                            };
                        }
                        break;
                    case FlashChipState.ChipIdentification:
                        if (address == 0x0E00_5555 && value == 0xF0) // Exit chip identification
                        {
                            _state = FlashChipState.Ready;
                        }
                        break;
                    case FlashChipState.Erasing:
                        if (address == 0x0E00_5555 && value == 0x10)
                        {
                            // Erase entire chip
                            Array.Fill(_data, (byte)0xFF); // Flash erases to 0xFF not 0x00
                            _state = FlashChipState.Ready;
                        }
                        else if ((address & 0xFFFF_0FFF) == 0x0E00_0000 && value == 0x30)
                        {
                            // Erase 4KB sector
                            Array.Fill(_data, (byte)0xFF, (int)(address & 0xF000) + (_bank * 0x1_0000), 0x1000);
                            _state = FlashChipState.Ready;
                        }
                        break;
                }

                _commandState = FlashCommandState.NotStarted;

                break;
        }
    }

    internal byte Read(uint address)
    {
        var maskedAddress = address & 0xFFFF;

        if (_state == FlashChipState.ChipIdentification)
        {
            if (maskedAddress == 0x0000)
            {
                return _manufacturerId;
            }
            else if (maskedAddress == 0x0001)
            {
                return _deviceId;
            }
        }

        return _data[(_bank * 0x1_0000) + maskedAddress];
    }

    public override string ToString() => $"{_commandState}, {_state}, Bank{_bank}";
}
