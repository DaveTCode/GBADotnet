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

    /// <summary>
    /// Some games check that RCNT has bit 8 set by the BIOS _despite_ that 
    /// bit not doing anything. Bloody Sonic.
    /// </summary>
    public ushort _rcnt = 0x8000;

    internal SerialController(BaseDebugger debugger, InterruptInterconnect interruptInterconnect)
    {
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _interruptInterconnect = interruptInterconnect ?? throw new ArgumentNullException(nameof(interruptInterconnect));
    }


    internal byte ReadByte(uint address) => address switch
    {
        RCNT => (byte)_rcnt,
        RCNT + 1 => (byte)(_rcnt >> 8),
        _ => 0, // TODO - Implement serial controller
    };

    internal ushort ReadHalfWord(uint address) => 
        (ushort)(ReadByte(address) | (ReadByte(address + 1) << 8));

    internal uint ReadWord(uint address) => 
        (uint)(ReadHalfWord(address) | (ReadHalfWord(address + 2) << 16));

    internal void WriteByte(uint address, byte value)
    {
        switch (address)
        {
            case RCNT:
                _rcnt = (ushort)((_rcnt & 0xFF0F) | (value & 0b1111_0000));
                break;
            case RCNT + 1:
                _rcnt = (ushort)((_rcnt & 0xFF00) | ((value & 0b0100_0001) << 8));
                break;
            case SIOCNT + 1:
                // TODO - This is a hack to pass AGS tests which want SIO interrupts
                if ((value & (1 << 6)) == (1 << 6))
                {
                    _interruptInterconnect.RaiseInterrupt(Interrupt.SerialCommunication);
                }
                break;
            default:
                // TODO
                break;
        }
    }

    internal void WriteHalfWord(uint address, ushort value)
    {
        WriteByte(address, (byte)value);
        WriteByte(address + 1, (byte)(value >> 8));
    }

    internal void WriteWord(uint address, uint value)
    {
        WriteHalfWord(address, (ushort)value);
        WriteHalfWord(address + 2, (ushort)(value >> 16));
    }
}
