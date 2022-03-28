using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Interrupts;

namespace GameboyAdvanced.Core.Serial;

/// <summary>
/// TODO - Not actually implemented any of this but some roms appear to expect register reads to work
/// </summary>
public class SerialController
{
    private readonly BaseDebugger _debugger;
    private readonly InterruptInterconnect _interruptInterconnect;

    internal SerialController(BaseDebugger debugger, InterruptInterconnect interruptInterconnect)
    {
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _interruptInterconnect = interruptInterconnect ?? throw new ArgumentNullException(nameof(interruptInterconnect));
    }


    internal byte ReadByte(uint address) => 0; // TODO - Implement serial controller

    internal ushort ReadHalfWord(uint address) => 0; // TODO - Implement serial controller

    internal uint ReadWord(uint address)
    {
        return (uint)(ReadHalfWord(address) | (ReadHalfWord(address + 2) << 16));
    }

    internal void WriteByte(uint address, byte value)
    {
        // TODO
    }

    internal void WriteHalfWord(uint address, ushort value)
    {
        // TODO
    }

    internal void WriteWord(uint address, uint value)
    {
        WriteHalfWord(address, (ushort)value);
        WriteHalfWord(address + 2, (ushort)(value >> 16));
    }
}
