using System.Text;

namespace GameboyAdvanced.Core;

public unsafe struct EventNode
{
    public EventType Type;
    public long Cycles;
    public delegate*<Device, void> Callback;

    public override string ToString() => $"{Type} - {Cycles}";
}

public enum EventType
{
    BreakHalt,
    CpuIrq,
    Timer0Irq,
    Timer0Overflow,
    Timer0Latch,
    Timer0LatchReload,
    Timer1Irq,
    Timer1Overflow,
    Timer1Latch,
    Timer1LatchReload,
    Timer2Irq,
    Timer2Overflow,
    Timer2Latch,
    Timer2LatchReload,
    Timer3Irq,
    Timer3Overflow,
    Timer3Latch,
    Timer3LatchReload,
    HBlankStart,
    HBlankEnd,
    Generic
}

public unsafe class Scheduler
{
    /// <summary>
    /// This is an upper bound of the number events, if we ever schedule 
    /// more events than this Very Bad Things will happen.
    /// </summary>
    private const int MaxEvents = 100;

    private readonly Device _device;
    private readonly EventNode[] _events = new EventNode[MaxEvents];
    private int _lastEventPtr;
    private int _nextEventPtr;

    internal Scheduler(Device device)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        Reset();
    }

    internal void Reset()
    {
        Array.Clear(_events);
        _lastEventPtr = -1;
        _nextEventPtr = 0;
    }

    /// <summary>
    /// O(n) where n is number of events way to cancel an event.
    /// 
    /// Used for example to cancel an existing timer overflow event when the 
    /// prescaler value changes causing overflow to happen at a different time.
    /// 
    /// Requires that a single event type can only exist once, not enforced 
    /// anywhere.
    /// </summary>
    internal void CancelEvent(EventType type)
    {
        for (var ii = _nextEventPtr; ii <= _lastEventPtr; ii++)
        {
            if (_events[ii].Type == type)
            {
                for (var jj = ii; jj < _lastEventPtr; jj++)
                {
                    _events[jj].Cycles = _events[jj + 1].Cycles;
                    _events[jj].Type = _events[jj + 1].Type;
                    _events[jj].Callback = _events[jj + 1].Callback;
                }
                _lastEventPtr--;
            }
        }
    }

    /// <summary>
    /// Given an event which is supposed to happen N cycles from the current 
    /// cycle, this function will insert it into the correct place within the
    /// event array.
    /// 
    /// This will ensure uniqueness of event as there should be no more than 
    /// one of each event type.
    /// </summary>
    internal void ScheduleEvent(EventType type, delegate*<Device, void> evt, long cyclesFromNow)
    {
        var absoluteCycles = _device.Cpu.Cycles + cyclesFromNow;

        // If we run out of space in the array then move events back to the
        // beginning on the array to rebalance
        if (_lastEventPtr == MaxEvents - 1)
        {
            for (var ii = _nextEventPtr; ii <= _lastEventPtr; ii++)
            {
                _events[ii - _nextEventPtr].Type = _events[ii].Type;
                _events[ii - _nextEventPtr].Callback = _events[ii].Callback;
                _events[ii - _nextEventPtr].Cycles = _events[ii].Cycles;
            }
            _lastEventPtr -= _nextEventPtr;
            _nextEventPtr = 0;
        }

        _lastEventPtr++;

        for (var ii = _lastEventPtr - 1; ii >= _nextEventPtr; ii--)
        {
            if (absoluteCycles <= _events[ii].Cycles)
            {
                _events[ii + 1].Type = _events[ii].Type;
                _events[ii + 1].Cycles = _events[ii].Cycles;
                _events[ii + 1].Callback = _events[ii].Callback;

                if (ii == _nextEventPtr)
                {
                    _events[ii].Type = type;
                    _events[ii].Cycles = absoluteCycles;
                    _events[ii].Callback = evt;
                    return;
                }
            }
            else
            {
                _events[ii + 1].Type = type;
                _events[ii + 1].Cycles = absoluteCycles;
                _events[ii + 1].Callback = evt;
                return;
            }
        }

        // Fall back code for if there are no events to check through
        _events[_nextEventPtr].Type = type;
        _events[_nextEventPtr].Cycles = absoluteCycles;
        _events[_nextEventPtr].Callback = evt;
    }

    internal bool EventScheduled(EventType type)
    {
        for (var ii = _nextEventPtr; ii <= _lastEventPtr; ii++)
        {
            if (_events[ii].Type == type)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Called each device main clock cycle, fires any events due on this 
    /// cycle and removes them from the list.
    /// </summary>
    internal void Step()
    {
        while (true)
        {
            var eventPtr = _nextEventPtr;

            if (eventPtr > _lastEventPtr) return; // No events left

            if (_events[eventPtr].Cycles == _device.Cpu.Cycles)
            {
                _nextEventPtr++;
                _events[eventPtr].Callback(_device);
            }
            else
            {
                return; // Next event is not yet
            }
        }
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        _ = result.AppendLine($"Scheduler from {_nextEventPtr}->{_lastEventPtr}");
        for (var ii = _nextEventPtr; ii <= _lastEventPtr; ii++)
        {
            _ = result.AppendLine(_events[ii].ToString());
        }
        return result.ToString();
    }
}
