using static GameboyAdvanced.Core.IORegs;
using GameboyAdvanced.Core.Debug;

namespace GameboyAdvanced.Core.Timer;

/// <summary>
/// The timer controller subsystem ticks on each clock cycle and is responsible 
/// for incrementing/decrementing timers and determining when to fire IRQs
/// </summary>
public unsafe class TimerController
{
    private readonly Device _device;
    private readonly BaseDebugger _debugger;
    public readonly TimerRegister[] _timers;

    internal TimerController(Device device, BaseDebugger debugger)
    {
        _device = device;
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _timers = new TimerRegister[4]
        {
            new TimerRegister(device, 0), new TimerRegister(device, 1), new TimerRegister(device, 2), new TimerRegister(device, 3)
        };
    }

    internal void Reset()
    {
        foreach (var timer in _timers)
        {
            timer.Reset();
        }
    }

    internal byte ReadByte(uint address)
    {
        var halfWord = ReadHalfWord(address & 0xFFFF_FFFE);
        return (byte)(halfWord >> (ushort)(8 * (address & 1)));
    }

    internal ushort ReadHalfWord(uint address) => address switch
    {
        TM0CNT_L => _timers[0].ReadCounter(),
        TM0CNT_H => _timers[0].ReadControl(),
        TM1CNT_L => _timers[1].ReadCounter(),
        TM1CNT_H => _timers[1].ReadControl(),
        TM2CNT_L => _timers[2].ReadCounter(),
        TM2CNT_H => _timers[2].ReadControl(),
        TM3CNT_L => _timers[3].ReadCounter(),
        TM3CNT_H => _timers[3].ReadControl(),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} not mapped for timers"),
    };

    internal uint ReadWord(uint address) => (uint)(ReadHalfWord(address) | (ReadHalfWord(address + 2) << 16));

    internal void WriteByte(uint address, byte value)
    {
        switch (address)
        {
            case TM0CNT_L:
                _timers[0].Reload = (ushort)((_timers[0].Reload & 0xFF00) | value);
                _device.Scheduler.ScheduleEvent(EventType.Timer0Latch, &TimerRegister.LatchTimer0ValuesEvent, 1);
                break;
            case TM0CNT_L + 1:
                _timers[0].Reload = (ushort)((_timers[0].Reload & 0x00FF) | (value << 8));
                _device.Scheduler.ScheduleEvent(EventType.Timer0Latch, &TimerRegister.LatchTimer0ValuesEvent, 1);
                break;
            case TM0CNT_H:
                _timers[0].UpdateControl(value);
                break;
            case TM1CNT_L:
                _timers[1].Reload = (ushort)((_timers[1].Reload & 0xFF00) | value);
                _device.Scheduler.ScheduleEvent(EventType.Timer1Latch, &TimerRegister.LatchTimer1ValuesEvent, 1);
                break;
            case TM1CNT_L + 1:
                _timers[1].Reload = (ushort)((_timers[1].Reload & 0x00FF) | (value << 8));
                _device.Scheduler.ScheduleEvent(EventType.Timer1Latch, &TimerRegister.LatchTimer1ValuesEvent, 1);
                break;
            case TM1CNT_H:
                _timers[1].UpdateControl(value);
                break;
            case TM2CNT_L:
                _timers[2].Reload = (ushort)((_timers[2].Reload & 0xFF00) | value);
                _device.Scheduler.ScheduleEvent(EventType.Timer2Latch, &TimerRegister.LatchTimer2ValuesEvent, 1);
                break;
            case TM2CNT_L + 1:
                _timers[2].Reload = (ushort)((_timers[2].Reload & 0x00FF) | (value << 8));
                _device.Scheduler.ScheduleEvent(EventType.Timer2Latch, &TimerRegister.LatchTimer2ValuesEvent, 1);
                break;
            case TM2CNT_H:
                _timers[2].UpdateControl(value);
                break;
            case TM3CNT_L:
                _timers[3].Reload = (ushort)((_timers[3].Reload & 0xFF00) | value);
                _device.Scheduler.ScheduleEvent(EventType.Timer3Latch, &TimerRegister.LatchTimer3ValuesEvent, 1);
                break;
            case TM3CNT_L + 1:
                _timers[3].Reload = (ushort)((_timers[3].Reload & 0x00FF) | (value << 8));
                _device.Scheduler.ScheduleEvent(EventType.Timer3Latch, &TimerRegister.LatchTimer3ValuesEvent, 1);
                break;
            case TM3CNT_H:
                _timers[3].UpdateControl(value);
                break;
            default:
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
