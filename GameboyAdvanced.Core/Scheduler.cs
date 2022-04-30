namespace GameboyAdvanced.Core;

public unsafe struct EventNode
{
    public long Cycles;
    public delegate*<Device, void> Callback;
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
    /// Given an event which is supposed to happen N cycles from the current 
    /// cycle, this function will insert it into the correct place within the
    /// event array.
    /// </summary>
    internal void ScheduleEvent(delegate*<Device, void> evt, long cyclesFromNow)
    {
        var absoluteCycles = _device.Cpu.Cycles + cyclesFromNow;

        // If we run out of space in the array then move events back to the
        // beginning on the array to rebalance
        if (_lastEventPtr == MaxEvents - 1)
        {
            for (var ii = _nextEventPtr; ii < _lastEventPtr; ii++)
            {
                _events[0].Callback = _events[ii].Callback;
                _events[0].Cycles = _events[ii].Cycles;
            }
            _lastEventPtr -= _nextEventPtr;
            _nextEventPtr = 0;
        }

        _lastEventPtr++;

        for (var ii = _lastEventPtr - 1; ii >= _nextEventPtr; ii--)
        {
            if (absoluteCycles <= _events[ii].Cycles)
            {
                _events[ii + 1].Cycles = _events[ii].Cycles;
                _events[ii + 1].Callback = _events[ii].Callback;

                if (ii == _nextEventPtr)
                {
                    _events[ii].Cycles = absoluteCycles;
                    _events[ii].Callback = evt;
                    return;
                }
            }
            else
            {
                _events[ii + 1].Cycles = absoluteCycles;
                _events[ii + 1].Callback = evt;
                return;
            }
        }

        // Fall back code for if there are no events to check through
        _events[_nextEventPtr].Cycles = absoluteCycles;
        _events[_nextEventPtr].Callback = evt;
    }

    /// <summary>
    /// Called each device main clock cycle, fires any events due on this 
    /// cycle and removes them from the list.
    /// </summary>
    internal void Step()
    {
        for (var ii = _nextEventPtr; ii <= _lastEventPtr; ii++)
        {
            if (_events[ii].Cycles == _device.Cpu.Cycles)
            {
                _events[ii].Callback(_device);
                _nextEventPtr++;
            }
            else
            {
                break;
            }
        }
    }
}
