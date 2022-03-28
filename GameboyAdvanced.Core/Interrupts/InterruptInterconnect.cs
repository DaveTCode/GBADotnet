using GameboyAdvanced.Core.Debug;

namespace GameboyAdvanced.Core.Interrupts;

internal enum Interrupt
{
    LCDVBlank = 0b0000_0000_0000_0001,
    LCDHBlank = 0b0000_0000_0000_0010,
    LCDVCounter = 0b0000_0000_0000_0100,
    Timer0Overflow = 0b0000_0000_0000_1000,
    Timer1Overflow = 0b0000_0000_0001_0000,
    Timer2Overflow = 0b0000_0000_0010_0000,
    Timer3Overflow = 0b0000_0000_0100_0000,
    SerialCommunication = 0b0000_0000_1000_0000,
    DMA0 = 0b0000_0001_0000_0000,
    DMA1 = 0b0000_0010_0000_0000,
    DMA2 = 0b0000_0100_0000_0000,
    DMA3 = 0b0000_1000_0000_0000,
    Keypad = 0b0001_0000_0000_0000,
    GamePak = 0b0010_0000_0000_0000,
}

/// <summary>
/// The InterruptInterconnect class is provided to other components as an 
/// interface through which they can raise interrupts.
/// 
/// In practice all this is doing is setting the appropriate bit to 1 on the 
/// IF register but to help with ownership concerns the two functions are split.
/// </summary>
public class InterruptInterconnect
{
    private readonly BaseDebugger _debugger;
    private readonly InterruptRegisters _registers;

    internal InterruptInterconnect(BaseDebugger debugger, InterruptRegisters registers)
    {
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _registers = registers ?? throw new ArgumentNullException(nameof(registers));
    }

    internal void RaiseInterrupt(Interrupt interrupt) => _registers.RaiseInterrupt(interrupt);

    internal bool CpuShouldIrq() => _registers.CpuShouldIrq;
}
