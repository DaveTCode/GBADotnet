using GameboyAdvanced.Core.Debug;
using static GameboyAdvanced.Core.IORegs;

namespace GameboyAdvanced.Core.Serial;

/// <summary>
/// TODO - Not actually implemented any of this but some roms appear to expect register reads to work
/// </summary>
internal class SerialController
{
    private readonly BaseDebugger _debugger;

    internal SerialController(BaseDebugger debugger)
    {
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
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
        _ => throw new Exception($"Serial controller doesn't map address {address:X8}"),
    };

    internal uint ReadWord(uint address) => address switch
    {
        JOY_RECV => 0x0,
        JOY_TRANS => 0x0,
        _ => throw new Exception($"Serial controller doesn't map address {address:X8} for word reads"),
    };

    internal void WriteByte(uint address, byte value) => throw new NotImplementedException("Byte writes to serial controller not yet implemented");

    internal void WriteHalfWord(uint address, ushort value)
    {
        // TODO - This is only implemented at all because both doom and mgba expect to write to RCNT
        switch (address)
        {
            case SIOCNT:
#if DEBUG
                _debugger.Log($"Write to SIOCNT={value:X4} which is not properly implemented");
#endif
                break;
            case RCNT:
#if DEBUG
                _debugger.Log($"Write to RCNT={value:X4} which is not properly implemented");
#endif
                break;
            default:
                throw new NotImplementedException("Serial controller is not really implemented");
        }
    }

    internal void WriteWord(uint address, uint value) => throw new NotImplementedException("Word writes to serial controller not yet implemented");
}
