using static GameboyAdvanced.Core.IORegs;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Interrupts;

namespace GameboyAdvanced.Core.Timer;

/// <summary>
/// The timer controller subsystem ticks on each clock cycle and is responsible 
/// for incrementing/decrementing timers and determining when to fire IRQs
/// </summary>
internal class TimerController
{
    private readonly BaseDebugger _debugger;
    private readonly InterruptInterconnect _interruptInterconnect;
    private readonly TimerRegister[] _timers = new TimerRegister[4] { new TimerRegister(0), new TimerRegister(1), new TimerRegister(2), new TimerRegister(3) };
    private readonly int[] _timerSteps = new int[4];
    private readonly HashSet<int> _runningTimerIxs = new();

    internal TimerController(BaseDebugger debugger, InterruptInterconnect interruptInterconnect)
    {
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _interruptInterconnect = interruptInterconnect ?? throw new ArgumentNullException(nameof(interruptInterconnect));
    }

    internal void Reset()
    {
        foreach (var timer in _timers)
        {
            timer.Reset();
        }
    }


    internal void Step()
    {
        foreach (var timerIx in _runningTimerIxs)
        {
            _timerSteps[timerIx]--;
            if (_timerSteps[timerIx] == 0)
            {
                _timerSteps[timerIx] = _timers[timerIx].PrescalerSelection.Cycles();
                _timers[timerIx].Counter++;

                if (_timers[timerIx].Counter == 0)
                {
                    _timers[timerIx].Counter = _timers[timerIx].Reload;

                    // TODO - Handle timer DMA

                    // TODO - Handle count-up timing
                }
            }
        }
    }

    internal byte ReadByte(uint address) => throw new NotImplementedException("Read byte not implemented for timer registers");

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
        _ => throw new ArgumentOutOfRangeException(nameof(address), "Address {address:X8} not mapped for timers"),
    };

    internal uint ReadWord(uint address) => (uint)(ReadHalfWord(address) | (ReadHalfWord(address + 2) << 16));

    internal void WriteByte(uint _address, byte _value) => throw new NotImplementedException("Write byte not implemented for timer registers");

    internal void WriteHalfWord(uint address, ushort value)
    {
        switch (address)
        {
            case TM0CNT_L:
                _timers[0].Reload = value;
                return;
            case TM0CNT_H:
                _timers[0].UpdateControl(value, _runningTimerIxs);
                _timerSteps[0] = _timers[0].PrescalerSelection.Cycles();
                return;
            case TM1CNT_L:
                _timers[1].Reload = value;
                return;
            case TM1CNT_H:
                _timers[1].UpdateControl(value, _runningTimerIxs);
                _timerSteps[1] = _timers[1].PrescalerSelection.Cycles();
                return;
            case TM2CNT_L:
                _timers[2].Reload = value;
                return;
            case TM2CNT_H:
                _timers[2].UpdateControl(value, _runningTimerIxs);
                _timerSteps[2] = _timers[2].PrescalerSelection.Cycles();
                return;
            case TM3CNT_L:
                _timers[3].Reload = value;
                return;
            case TM3CNT_H:
                _timers[3].UpdateControl(value, _runningTimerIxs);
                _timerSteps[3] = _timers[3].PrescalerSelection.Cycles();
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(address), "Address {address:X8} not mapped for timers");
        }
    }

    internal void WriteWord(uint address, uint value)
    {
        WriteHalfWord(address, (ushort)value);
        WriteHalfWord(address + 2, (ushort)(value >> 16));
    }
}
