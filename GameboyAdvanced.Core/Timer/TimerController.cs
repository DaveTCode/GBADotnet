using static GameboyAdvanced.Core.IORegs;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Interrupts;

namespace GameboyAdvanced.Core.Timer;

/// <summary>
/// The timer controller subsystem ticks on each clock cycle and is responsible 
/// for incrementing/decrementing timers and determining when to fire IRQs
/// </summary>
public unsafe class TimerController
{
    private readonly BaseDebugger _debugger;
    private readonly InterruptInterconnect _interruptInterconnect;
    private readonly TimerRegister[] _timers = new TimerRegister[4]
    {
        new TimerRegister(0), new TimerRegister(1), new TimerRegister(2), new TimerRegister(3)
    };
    private readonly int[] _timerSteps = new int[4];

    internal TimerController(BaseDebugger debugger, InterruptInterconnect interruptInterconnect)
    {
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _interruptInterconnect = interruptInterconnect ?? throw new ArgumentNullException(nameof(interruptInterconnect));
    }

    internal void Step()
    {
        var timerReloaded = new bool[4];

        // TODO - Very inefficient timer implementation ticking on each clock cycle, shown to take up 20% of cpu time and could be offloaded to scheduler
        for (var ix = 0; ix < _timers.Length; ix++)
        {
            if (_timers[ix].Start || _timers[ix].CyclesToStop > 0)
            {
                // Emulate startup delay
                if (_timers[ix].CyclesToStart > 0)
                {
                    _timers[ix].CyclesToStart--;
                    continue;
                }

                if (_timers[ix].ReloadNeedsLatch)
                {
                    // The timer latches the reload value the cycle before it actually wraps
                    // Thanks to Fleroviux for working this out
                    _timers[ix].ReloadLatch = _timers[ix].Reload;
                    _timers[ix].ReloadNeedsLatch = false;
                }

                // Emulate stop delay (from register write there's one more tick)
                if (_timers[ix].CyclesToStop > 0)
                {
                    _timers[ix].CyclesToStop--;
                }

                if (_timers[ix].CountUpTiming && ix > 0) // Count up timing on TIMER0 is ignored (TODO - is it? Or does it hang?)
                {
                    if (timerReloaded[ix - 1])
                    {
                        timerReloaded[ix] = TickTimer(ref _timers[ix], ix);
                    }
                }
                else
                {
                    _timerSteps[ix]--;
                    if (_timerSteps[ix] == 0)
                    {
                        _timerSteps[ix] = _timers[ix].PrescalerSelection.Cycles();
                        timerReloaded[ix] = TickTimer(ref _timers[ix], ix);
                    }
                }
            }
        }
    }

    internal void Reset()
    {
        foreach (var timer in _timers)
        {
            timer.Reset();
        }
    }

    private bool TickTimer(ref TimerRegister timer, int ix)
    {
        timer.Counter++;

        if (timer.Counter == 0)
        {
            timer.Counter = timer.ReloadLatch;

            if (timer.IrqEnabled)
            {
                _interruptInterconnect.RaiseInterrupt(ix switch
                {
                    0 => Interrupt.Timer0Overflow,
                    1 => Interrupt.Timer1Overflow,
                    2 => Interrupt.Timer2Overflow,
                    3 => Interrupt.Timer3Overflow,
                    _ => throw new Exception("Invalid timer ix"),
                });
            }

            return true;
        }

        return false;
    }

    internal byte ReadByte(uint address)
    {
        var halfWord = ReadHalfWord(address & 0xFFFF_FFFE);
        return (byte)(halfWord >> (ushort)(8 * (address & 1)));
    }

    internal ushort ReadHalfWord(uint address) => address switch
    {
        TM0CNT_L => _timers[0].Counter,
        TM0CNT_H => _timers[0].ReadControl(),
        TM1CNT_L => _timers[1].Counter,
        TM1CNT_H => _timers[1].ReadControl(),
        TM2CNT_L => _timers[2].Counter,
        TM2CNT_H => _timers[2].ReadControl(),
        TM3CNT_L => _timers[3].Counter,
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
                _timers[0].ReloadNeedsLatch = true;
                break;
            case TM0CNT_L + 1:
                _timers[0].Reload = (ushort)((_timers[0].Reload & 0x00FF) | (value << 8));
                _timers[0].ReloadNeedsLatch = true;
                break;
            case TM0CNT_H:
                _timers[0].UpdateControl(value);
                _timerSteps[0] = _timers[0].PrescalerSelection.Cycles();
                break;
            case TM1CNT_L:
                _timers[1].Reload = (ushort)((_timers[1].Reload & 0xFF00) | value);
                _timers[1].ReloadNeedsLatch = true;
                break;
            case TM1CNT_L + 1:
                _timers[1].Reload = (ushort)((_timers[1].Reload & 0x00FF) | (value << 8));
                _timers[1].ReloadNeedsLatch = true;
                break;
            case TM1CNT_H:
                _timers[1].UpdateControl(value);
                _timerSteps[1] = _timers[1].PrescalerSelection.Cycles();
                break;
            case TM2CNT_L:
                _timers[2].Reload = (ushort)((_timers[2].Reload & 0xFF00) | value);
                _timers[2].ReloadNeedsLatch = true;
                break;
            case TM2CNT_L + 1:
                _timers[2].Reload = (ushort)((_timers[2].Reload & 0x00FF) | (value << 8));
                _timers[2].ReloadNeedsLatch = true;
                break;
            case TM2CNT_H:
                _timers[2].UpdateControl(value);
                _timerSteps[2] = _timers[2].PrescalerSelection.Cycles();
                break;
            case TM3CNT_L:
                _timers[3].Reload = (ushort)((_timers[3].Reload & 0xFF00) | value);
                _timers[3].ReloadNeedsLatch = true;
                break;
            case TM3CNT_L + 1:
                _timers[3].Reload = (ushort)((_timers[3].Reload & 0x00FF) | (value << 8));
                _timers[3].ReloadNeedsLatch = true;
                break;
            case TM3CNT_H:
                _timers[3].UpdateControl(value);
                _timerSteps[3] = _timers[3].PrescalerSelection.Cycles();
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
