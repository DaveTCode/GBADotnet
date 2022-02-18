namespace GameboyAdvanced.Core.Debug;

/// <summary>
/// The debug events are the various different events that can be hooked 
/// for breakpoints.
/// </summary>
public enum DebugEvent
{
    /// <summary>
    /// Branching to zero really only happens when a register gets splatted and a BX is called
    /// This is almost always going to be a mistake and having a debug event for it allows
    /// catching broken code in time to see what the call stack was.
    /// </summary>
    BranchToZero,
    SwitchToThumb,
    SwitchToArm,
}
