﻿using static GameboyAdvanced.Core.IORegs;
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
    public readonly TimerRegister[] _timers = new TimerRegister[4]
    {
        new TimerRegister(0), new TimerRegister(1), new TimerRegister(2), new TimerRegister(3)
    };
    public readonly int[] _timerSteps = new int[4];

    internal TimerController(BaseDebugger debugger, InterruptInterconnect interruptInterconnect)
    {
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _interruptInterconnect = interruptInterconnect ?? throw new ArgumentNullException(nameof(interruptInterconnect));
    }

    // Outside the function as otherwise this causes a malloc on each call and takes ~13% total CPU time for timer controller
    private readonly bool[] _timersReloaded = new bool[4];
    internal void Step()
    {
        // TODO - Very inefficient timer implementation ticking on each clock cycle, shown to take up 20% of cpu time and could be offloaded to scheduler
        for (var ix = 0; ix < _timers.Length; ix++)
        {
            _timersReloaded[ix] = false;
            if (_timers[ix].StartLatch)
            {
                // Emulate startup delay
                if (_timers[ix].CyclesToStart > 0)
                {
                    _timers[ix].CyclesToStart--;
                    continue;
                }

                if (_timers[ix].CountUpTimingLatch && ix > 0) // Count up timing on TIMER0 is ignored (TODO - is it? Or does it hang?)
                {
                    if (_timersReloaded[ix - 1])
                    {
                        _timersReloaded[ix] = TickTimer(ref _timers[ix], ix);
                    }
                }
                else
                {
                    _timerSteps[ix]--;
                    if (_timerSteps[ix] == 0)
                    {
                        _timerSteps[ix] = _timers[ix].PrescalerSelectionLatch.Cycles();
                        _timersReloaded[ix] = TickTimer(ref _timers[ix], ix);
                    }
                }
            }

            if (_timers[ix].NeedsLatch)
            {
                // The timer latches the reload value the cycle before it actually wraps
                // Thanks to Fleroviux for working this out
                _timers[ix].LatchValues(ref _timerSteps[ix]);
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

            if (timer.IrqEnabledLatch)
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
                _timers[0].NeedsLatch = true;
                break;
            case TM0CNT_L + 1:
                _timers[0].Reload = (ushort)((_timers[0].Reload & 0x00FF) | (value << 8));
                _timers[0].NeedsLatch = true;
                break;
            case TM0CNT_H:
                _timers[0].UpdateControl(value);
                break;
            case TM1CNT_L:
                _timers[1].Reload = (ushort)((_timers[1].Reload & 0xFF00) | value);
                _timers[1].NeedsLatch = true;
                break;
            case TM1CNT_L + 1:
                _timers[1].Reload = (ushort)((_timers[1].Reload & 0x00FF) | (value << 8));
                _timers[1].NeedsLatch = true;
                break;
            case TM1CNT_H:
                _timers[1].UpdateControl(value);
                break;
            case TM2CNT_L:
                _timers[2].Reload = (ushort)((_timers[2].Reload & 0xFF00) | value);
                _timers[2].NeedsLatch = true;
                break;
            case TM2CNT_L + 1:
                _timers[2].Reload = (ushort)((_timers[2].Reload & 0x00FF) | (value << 8));
                _timers[2].NeedsLatch = true;
                break;
            case TM2CNT_H:
                _timers[2].UpdateControl(value);
                break;
            case TM3CNT_L:
                _timers[3].Reload = (ushort)((_timers[3].Reload & 0xFF00) | value);
                _timers[3].NeedsLatch = true;
                break;
            case TM3CNT_L + 1:
                _timers[3].Reload = (ushort)((_timers[3].Reload & 0x00FF) | (value << 8));
                _timers[3].NeedsLatch = true;
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
