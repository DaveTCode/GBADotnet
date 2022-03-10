using static GameboyAdvanced.Core.IORegs;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Interrupts;

namespace GameboyAdvanced.Core.Timer;

/// <summary>
/// The timer controller subsystem ticks on each clock cycle and is responsible 
/// for incrementing/decrementing timers and determining when to fire IRQs
/// </summary>
internal unsafe class TimerController
{
    private readonly BaseDebugger _debugger;
    private readonly InterruptInterconnect _interruptInterconnect;
    private readonly TimerRegister[] _timers = new TimerRegister[4] { new TimerRegister(0), new TimerRegister(1), new TimerRegister(2), new TimerRegister(3) };
    private readonly int[] _timerSteps = new int[4];
    private delegate*<TimerController, void> _timerFunction = &StepNoop;

    internal TimerController(BaseDebugger debugger, InterruptInterconnect interruptInterconnect)
    {
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _interruptInterconnect = interruptInterconnect ?? throw new ArgumentNullException(nameof(interruptInterconnect));
    }

    internal void Step()
    {
        _timerFunction(this);
    }

    internal void Reset()
    {
        foreach (var timer in _timers)
        {
            timer.Reset();
        }
    }

    internal static void StepNoop(TimerController _) { }

    internal static void StepOp(TimerController ctrl)
    {
        // TODO - How to avoid this for loop when no timers enabled?
        for (var ix = 0; ix < ctrl._timers.Length; ix++)
        {
            if (ctrl._timers[ix].Start)
            {
                // Emulate startup delay
                if (ctrl._timers[ix].CyclesToStart > 0)
                {
                    ctrl._timers[ix].CyclesToStart--;
                    continue;
                }

                if (ctrl._timers[ix].CountUpTiming && ix > 0) // Count up timing on TIMER0 is ignored (TODO - is it? Or does it hang?)
                {
                    if (ctrl._timers[ix - 1].Start && ctrl._timers[ix - 1].Counter == ctrl._timers[ix - 1].Reload)
                    {
                        ctrl.TickTimer(ref ctrl._timers[ix], ix);
                    }
                }
                else
                {
                    ctrl._timerSteps[ix]--;
                    if (ctrl._timerSteps[ix] == 0)
                    {
                        ctrl._timerSteps[ix] = ctrl._timers[ix].PrescalerSelection.Cycles();
                        ctrl.TickTimer(ref ctrl._timers[ix], ix);
                    }
                }
            }
        }
    }

    private void TickTimer(ref TimerRegister timer, int ix)
    {
        timer.Counter++;

        if (timer.Counter == 0)
        {
            timer.Counter = timer.Reload;

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
        }
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

    internal void WriteByte(uint _address, byte _value) => throw new NotImplementedException("Write byte not implemented for timer registers");

    internal void WriteHalfWord(uint address, ushort value)
    {
        switch (address)
        {
            case TM0CNT_L:
                _timers[0].Reload = value;
                break;
            case TM0CNT_H:
                _timers[0].UpdateControl(value);
                _timerSteps[0] = _timers[0].PrescalerSelection.Cycles();
                break;
            case TM1CNT_L:
                _timers[1].Reload = value;
                break;
            case TM1CNT_H:
                _timers[1].UpdateControl(value);
                _timerSteps[1] = _timers[1].PrescalerSelection.Cycles();
                break;
            case TM2CNT_L:
                _timers[2].Reload = value;
                break;
            case TM2CNT_H:
                _timers[2].UpdateControl(value);
                _timerSteps[2] = _timers[2].PrescalerSelection.Cycles();
                break;
            case TM3CNT_L:
                _timers[3].Reload = value;
                break;
            case TM3CNT_H:
                _timers[3].UpdateControl(value);
                _timerSteps[3] = _timers[3].PrescalerSelection.Cycles();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} not mapped for timers");
        }

        CheckEnableTimerFunction();
    }

    internal void CheckEnableTimerFunction()
    {
        for (var ii = 0; ii < 4; ii++)
        {
            if (_timers[ii].Start)
            {
                _timerFunction = &StepOp;
                return;
            }
        }

        _timerFunction = &StepNoop;
    }

    internal void WriteWord(uint address, uint value)
    {
        WriteHalfWord(address, (ushort)value);
        WriteHalfWord(address + 2, (ushort)(value >> 16));
    }
}
