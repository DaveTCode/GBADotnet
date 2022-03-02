using GameboyAdvanced.Core.Debug;
using Serilog;

namespace GameboyAdvanced.Sdl2;

/// <summary>
/// When the GameboyAdvanced.Core class library is built in debug mode a 
/// debugger is attached to the <see cref="Device"/> object.
/// 
/// This debugger can have breakpoints registered with it. If the event
/// causing a breakpoint will occur on the current cycle (but before it
/// has executed) then a <see cref="BreakpointException"/> will be thrown
/// by the <see cref="Device.RunCycle"/> function allowing the calling 
/// code to decide what to do.
/// </summary>
public class Debugger : BaseDebugger
{
    private readonly ILogger _logger;

    internal Debugger(ILogger logger)
    {
        _logger = logger;
    }

    public override void Log(string message, params object[] vars)
    {
        _logger.Debug(message, vars);
    }
}
