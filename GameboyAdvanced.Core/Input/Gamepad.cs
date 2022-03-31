using static GameboyAdvanced.Core.IORegs;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Interrupts;

namespace GameboyAdvanced.Core.Input;

public class Gamepad
{
    public readonly Dictionary<Key, bool> _keyIrq = new()
    {
        { Key.A, false },
        { Key.B, false },
        { Key.Select, false },
        { Key.Start, false },
        { Key.Right, false },
        { Key.Left, false },
        { Key.Up, false },
        { Key.Down, false },
        { Key.L, false },
        { Key.R, false },
    };

    public readonly Dictionary<Key, bool> _keyPressed = new()
    {
        { Key.A, false },
        { Key.B, false },
        { Key.Select, false },
        { Key.Start, false },
        { Key.Right, false },
        { Key.Left, false },
        { Key.Up, false },
        { Key.Down, false },
        { Key.L, false },
        { Key.R, false },
    };

    private readonly BaseDebugger _debugger;
    private readonly InterruptInterconnect _interruptInterconnect;

    public bool _irqEnabled;
    public bool _irqConditionAnd;

    internal Gamepad(BaseDebugger debugger, InterruptInterconnect interruptInterconnect)
    {
        _debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
        _interruptInterconnect = interruptInterconnect ?? throw new ArgumentNullException(nameof(interruptInterconnect));
    }

    private ushort KeyStatusRegister() => (ushort)(
        (_keyPressed[Key.A] ? 0 : (1 << 0)) |
        (_keyPressed[Key.B] ? 0 : (1 << 1)) |
        (_keyPressed[Key.Select] ? 0 : (1 << 2)) |
        (_keyPressed[Key.Start] ? 0 : (1 << 3)) |
        (_keyPressed[Key.Right] ? 0 : (1 << 4)) |
        (_keyPressed[Key.Left] ? 0 : (1 << 5)) |
        (_keyPressed[Key.Up] ? 0 : (1 << 6)) |
        (_keyPressed[Key.Down] ? 0 : (1 << 7)) |
        (_keyPressed[Key.R] ? 0 : (1 << 8)) |
        (_keyPressed[Key.L] ? 0 : (1 << 9)));

    private ushort KeyInterruptControl() => (ushort)(
        (_keyIrq[Key.A] ? (1 << 0) : 0) |
        (_keyIrq[Key.B] ? (1 << 1) : 0) |
        (_keyIrq[Key.Select] ? (1 << 2) : 0) |
        (_keyIrq[Key.Start] ? (1 << 3) : 0) |
        (_keyIrq[Key.Right] ? (1 << 4) : 0) |
        (_keyIrq[Key.Left] ? (1 << 5) : 0) |
        (_keyIrq[Key.Up] ? (1 << 6) : 0) |
        (_keyIrq[Key.Down] ? (1 << 7) : 0) |
        (_keyIrq[Key.R] ? (1 << 8) : 0) |
        (_keyIrq[Key.L] ? (1 << 9) : 0) |
        (_irqEnabled ? (1 << 14) : 0) |
        (_irqConditionAnd ? (1 << 15) : 0));

    internal void Reset()
    {
        _irqEnabled = false;
        _irqConditionAnd = false;
        foreach (var k in _keyPressed.Keys)
        {
            _keyPressed[k] = false;
        }
        foreach (var k in _keyIrq.Keys)
        {
            _keyIrq[k] = false;
        }
    }

    internal byte ReadByte(uint address) => (byte)(ReadHalfWord(address & 0xFFFF_FFFE) >> (int)(8 * (address & 1)));

    internal ushort ReadHalfWord(uint address) => address switch
    {
        KEYINPUT => KeyStatusRegister(),
        KEYCNT => KeyInterruptControl(),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is not mapped to Gamepad memory space"),
    };

    internal void WriteByte(uint address, byte value)
    {
        switch (address)
        {
            case KEYCNT:
                _keyIrq[Key.A] = (value & (1 << 0)) == (1 << 0);
                _keyIrq[Key.B] = (value & (1 << 1)) == (1 << 1);
                _keyIrq[Key.Select] = (value & (1 << 2)) == (1 << 2);
                _keyIrq[Key.Start] = (value & (1 << 3)) == (1 << 3);
                _keyIrq[Key.Right] = (value & (1 << 4)) == (1 << 4);
                _keyIrq[Key.Left] = (value & (1 << 5)) == (1 << 5);
                _keyIrq[Key.Up] = (value & (1 << 6)) == (1 << 6);
                _keyIrq[Key.Down] = (value & (1 << 7)) == (1 << 7);
                
                CheckIrqs();
                break;
            case KEYCNT + 1:
                _keyIrq[Key.R] = (value & (1 << 0)) == (1 << 0);
                _keyIrq[Key.L] = (value & (1 << 1)) == (1 << 1);
                _irqEnabled = (value & (1 << 6)) == (1 << 6);
                _irqConditionAnd = (value & (1 << 7)) == (1 << 7);

                CheckIrqs();
                break;
            default:
                break;
        }
    }

    internal void WriteHalfWord(uint address, ushort value)
    {
        if (address == KEYCNT)
        {
            _keyIrq[Key.A] = (value & (1 << 0)) == (1 << 0);
            _keyIrq[Key.B] = (value & (1 << 1)) == (1 << 1);
            _keyIrq[Key.Select] = (value & (1 << 2)) == (1 << 2);
            _keyIrq[Key.Start] = (value & (1 << 3)) == (1 << 3);
            _keyIrq[Key.Right] = (value & (1 << 4)) == (1 << 4);
            _keyIrq[Key.Left] = (value & (1 << 5)) == (1 << 5);
            _keyIrq[Key.Up] = (value & (1 << 6)) == (1 << 6);
            _keyIrq[Key.Down] = (value & (1 << 7)) == (1 << 7);
            _keyIrq[Key.R] = (value & (1 << 8)) == (1 << 8);
            _keyIrq[Key.L] = (value & (1 << 9)) == (1 << 9);
            _irqEnabled = (value & (1 << 14)) == (1 << 14);
            _irqConditionAnd = (value & (1 << 15)) == (1 << 15);
            
            CheckIrqs();
        }
    }

    internal void PressKey(Key key)
    {
        _keyPressed[key] = true;

        CheckIrqs();
    }

    internal void ReleaseKey(Key key)
    {
        _keyPressed[key] = false;

        CheckIrqs();
    }

    internal void CheckIrqs()
    {
        if (_irqEnabled)
        {
            var shouldIrq = true;
            if (_irqConditionAnd)
            {
                for (var ii = 0; ii < 9; ii++)
                {
                    if (_keyIrq[(Key)ii])
                    {
                        if (!_keyPressed[(Key)ii])
                        {
                            shouldIrq = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                for (var ii = 0; ii < 9; ii++)
                {
                    shouldIrq = false;
                    if (_keyIrq[(Key)ii] && _keyPressed[(Key)ii])
                    {
                        shouldIrq = true;
                        break;
                    }
                }
            }

            if (shouldIrq)
            {
                _interruptInterconnect.RaiseInterrupt(Interrupt.Keypad);
            }
        }
    }
}
