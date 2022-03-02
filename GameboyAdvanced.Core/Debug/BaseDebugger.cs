namespace GameboyAdvanced.Core.Debug;

public class TestDebugger : BaseDebugger
{
    public override void Log(string messageString, params object[] vars)
    {
    }
}

public abstract class BaseDebugger
{
    public abstract void Log(string messageString, params object[] vars);

    private readonly HashSet<uint> _codeBreakpoints = new();
    private readonly HashSet<uint> _readBreakpoints = new();
    private readonly HashSet<(uint?, uint?)> _writeBreakpoints = new();
    private readonly HashSet<(int, uint)> _regBreakpoints = new();
    private readonly HashSet<DebugEvent> _debugEvents = new();
    private bool _waitingForNextInstruction = false;
    private bool _forceBreakNextCycle = false;

    public void RegisterExecuteBreakpoint(uint address) => _codeBreakpoints.Add(address);

    public void RegisterReadBreakpoint(uint address) => _readBreakpoints.Add(address);

    public void RegisterWriteBreakpoint(uint? address, uint? value) => _writeBreakpoints.Add((address, value));

    public void RegisterDebugEvent(DebugEvent e) => _debugEvents.Add(e);

    public void RegisterRegValBreakpoint(int reg, uint val) => _regBreakpoints.Add((reg, val));

    internal bool BreakOnExecute(uint address) => _codeBreakpoints.Contains(address);

    internal void FireEvent(DebugEvent e, Core core)
    {
        // TODO - Better way to register events
        //Console.WriteLine($"Event {e}\n{core}");
    }

    internal bool CheckBreakpoints(Core core)
    {
        if (_forceBreakNextCycle)
        {
            _forceBreakNextCycle = false;
            return true;
        }

        // Check for memory read access breakpoints (includes opcode reads at present)
        if (!core.nRW && core.nMREQ && _readBreakpoints.Contains(core.A))
        {
            return true;
        }

        // Check for memory write access breakpoints
        if (core.nRW && core.nMREQ)
        {
            foreach (var (a, d) in _writeBreakpoints)
            {
                if (!a.HasValue && !d.HasValue) continue;
                var brk = true;
                if (a.HasValue && core.A != a.Value) brk = false;
                if (d.HasValue && core.D != d.Value) brk = false;
                return brk;
            }
        }

        // Check for reg value breakpoints
        foreach (var (r, v) in _regBreakpoints)
        {
            if (core.R[r] == v) return true;
        }

        return false;
    }

    /// <summary>
    /// Calling this will cause the debugger to throw a BreakpointException the
    /// next time that a cpu cycle will include the first execute cycle of an 
    /// instruction.
    /// </summary>
    public void BreakOnNextInstruction()
    {
        _waitingForNextInstruction = true;
    }

    internal bool CheckBreakOnNextInstruction()
    {
        var ret = _waitingForNextInstruction;
        _waitingForNextInstruction = false;
        return ret;
    }

    internal void ForceBreakpointNextCycle()
    {
        _forceBreakNextCycle = true;
    }
}