namespace GameboyAdvanced.Core.Timer;

/// <summary>
/// The timer controller subsystem ticks on each clock cycle and is responsible 
/// for incrementing/decrementing timers and determining when to fire IRQs
/// </summary>
internal class TimerController
{
    private readonly TimerRegister[] _timers = new TimerRegister[4] { new TimerRegister(), new TimerRegister(), new TimerRegister(), new TimerRegister() };

    internal void Reset()
    {
        for (var ii = 0; ii < _timers.Length; ii++)
        {
            _timers[ii] = new TimerRegister();
        }
    }


    internal void Step(int cycles)
    {
        // TODO - Implement timers
    }

    internal (byte, int) ReadByte(uint address) => throw new NotImplementedException("Read byte not implemented for timer registers");

    internal (ushort, int) ReadHalfWord(uint address) => address switch
    {
        0x0400_0100 => (_timers[0].Reload, 1),
        0x0400_0102 => (_timers[0].ReadControl(), 1),
        0x0400_0104 => (_timers[1].Reload, 1),
        0x0400_0106 => (_timers[1].ReadControl(), 1),
        0x0400_0108 => (_timers[2].Reload, 1),
        0x0400_010A => (_timers[2].ReadControl(), 1),
        0x0400_010C => (_timers[3].Reload, 1),
        0x0400_010E => (_timers[3].ReadControl(), 1),
        _ => throw new ArgumentOutOfRangeException(nameof(address), "Address {address:X8} not mapped for timers"),
    };

    internal (uint, int) ReadWord(uint address) => address switch
    {
        0x0400_0100 => ((uint)(_timers[0].Reload << 16) | _timers[0].ReadControl(), 1),
        0x0400_0104 => ((uint)(_timers[1].Reload << 16) | _timers[1].ReadControl(), 1),
        0x0400_0108 => ((uint)(_timers[2].Reload << 16) | _timers[2].ReadControl(), 1),
        0x0400_010C => ((uint)(_timers[3].Reload << 16) | _timers[3].ReadControl(), 1),
        _ => throw new ArgumentOutOfRangeException(nameof(address), "Address {address:X8} not mapped for timers"),
    };

    internal int WriteByte(uint _address, byte _value) => throw new NotImplementedException("Write byte not implemented for timer registers");

    internal int WriteHalfWord(uint address, ushort value)
    {
        switch (address)
        {
            case 0x0400_0100:
                _timers[0].Reload = value;
                return 1;
            case 0x0400_0102:
                _timers[0].UpdateControl(value);
                return 1;
            case 0x0400_0104:
                _timers[1].Reload = value;
                return 1;
            case 0x0400_0106:
                _timers[1].UpdateControl(value);
                return 1;
            case 0x0400_0108:
                _timers[2].Reload = value;
                return 1;
            case 0x0400_010A:
                _timers[2].UpdateControl(value);
                return 1;
            case 0x0400_010C:
                _timers[3].Reload = value;
                return 1;
            case 0x0400_010E:
                _timers[3].UpdateControl(value);
                return 1;
            default:
                throw new ArgumentOutOfRangeException(nameof(address), "Address {address:X8} not mapped for timers");
        }
    }

    internal int WriteWord(uint address, uint value) =>
        WriteHalfWord(address, (ushort)(value >> 16)) +
        WriteHalfWord(address + 2, (ushort)value);
}
