using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Interrupts;
using static GameboyAdvanced.Core.IORegs;

namespace GameboyAdvanced.Core.Serial;

/// <summary>
/// TODO - Not actually implemented any of this but some roms appear to expect register reads to work
/// </summary>
internal class SerialController
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
        _ => throw new Exception($"Serial controller doesn't map address {address:X8}"),
    };

    internal uint ReadWord(uint address) => address switch
    {
        JOY_RECV => 0x0,
        JOY_TRANS => 0x0,
        _ => throw new Exception($"Serial controller doesn't map address {address:X8} for word reads"),
    };

    internal void WriteByte(uint address, byte value)
    {
        switch (address)
        {
            case SIODATA32:
            case SIOMULTI1:
            case SIOMULTI2:
            case SIOMULTI3:
            case SIOCNT:
            case SIODATA8:
            case RCNT:
            case JOYCNT:
            case JOY_RECV:
            case JOY_TRANS:
            case JOYSTAT:
                // TODO - Actually do something with these
#if DEBUG
                _debugger.Log($"Byte write to {address:X8}={value:X4} but serial registers aren't doing anything yet");
#endif
                return;
            default:
                throw new Exception($"Invalid address {address:X8} for serial controller write byte");
        }
    }

    internal void WriteHalfWord(uint address, ushort value)
    {
        // TODO - This is only implemented at all because both doom and mgba expect to write to RCNT
        switch (address)
        {
            case SIODATA32:
            case SIOMULTI1:
            case SIOMULTI2:
            case SIOMULTI3:
            case SIOCNT:
            case SIODATA8:
            case RCNT:
            case JOYCNT:
            case JOY_RECV:
            case JOY_TRANS:
            case JOYSTAT:
                // TODO - Actually do something with these
#if DEBUG
                _debugger.Log($"Half word write to {address:X8}={value:X4} but serial registers aren't doing anything yet");
#endif
                return;
            default:
                throw new Exception($"Invalid address {address:X8} for serial controller write half word");
        }
    }

    internal void WriteWord(uint address, uint value)
    {
        switch (address)
        {
            case SIODATA32:
            case SIOMULTI1:
            case SIOMULTI2:
            case SIOMULTI3:
            case SIOCNT:
            case SIODATA8:
            case RCNT:
            case JOYCNT:
            case JOY_RECV:
            case JOY_TRANS:
            case JOYSTAT:
                // TODO - Actually do something with these
#if DEBUG
                _debugger.Log($"Half word write to {address:X8}={value:X4} but serial registers aren't doing anything yet");
#endif
                return;
            default:
                throw new Exception($"Invalid address {address:X8} for serial controller write word");
        }
    }
}
