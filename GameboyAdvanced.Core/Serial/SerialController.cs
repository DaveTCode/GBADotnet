using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Interrupts;
using static GameboyAdvanced.Core.IORegs;

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


    internal byte ReadByte(uint address) => throw new NotImplementedException("Byte reads from serial controller not implemented");

    internal ushort ReadHalfWord(uint address) => address switch
    {
        SIODATA32 => 0x0,
        SIOMULTI1 => 0x0,
        SIOMULTI2 => 0x0,
        SIOMULTI3 => 0x0,
        SIOCNT => 0x0,
        SIODATA8 => 0x0,
        RCNT => 0x0,
        JOYCNT => 0x0,
        JOY_RECV => throw new NotImplementedException("Joypad reads are probably supposed to be word reads"),
        JOY_TRANS => throw new NotImplementedException("Joypad reads are probably supposed to be word reads"),
        JOYSTAT => throw new NotImplementedException("Joypad reads are probably supposed to be word reads"),
        0x0400_012C => 0, // Unused address on serial controller which gets read/written by various games during startup
        _ => throw new Exception($"Serial controller doesn't map address {address:X8} for halfword reads"), // TODO - Not 100% sure this is right
    };

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
