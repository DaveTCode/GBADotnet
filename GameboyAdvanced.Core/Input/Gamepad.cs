namespace GameboyAdvanced.Core.Input;

internal class Gamepad
{
    private readonly Dictionary<Key, bool> _keyIrq = new()
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

    private readonly Dictionary<Key, bool> _keyPressed = new()
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

    // TODO - Actually do something with IRQs from input
    private bool _irqEnabled;
    private bool _irqConditionAnd;

    private ushort KeyStatusRegister() => (ushort)(
        (_keyPressed[Key.A] ? (1 << 0) : 0) |
        (_keyPressed[Key.B] ? (1 << 1) : 0) |
        (_keyPressed[Key.Select] ? (1 << 2) : 0) |
        (_keyPressed[Key.Start] ? (1 << 3) : 0) |
        (_keyPressed[Key.Right] ? (1 << 4) : 0) |
        (_keyPressed[Key.Left] ? (1 << 5) : 0) |
        (_keyPressed[Key.Up] ? (1 << 6) : 0) |
        (_keyPressed[Key.Down] ? (1 << 7) : 0) |
        (_keyPressed[Key.R] ? (1 << 8) : 0) |
        (_keyPressed[Key.L] ? (1 << 9) : 0));

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

    internal (byte, int) ReadByte(uint address) => throw new NotImplementedException("Read byte not implemented for gamepad registers");
    internal (uint, int) ReadWord(uint address) => throw new NotImplementedException("Read word not implemented for gamepad registers");

    internal (ushort, int) ReadHalfWord(uint address) => address switch
    {
        0x0400_0130 => (KeyStatusRegister(), 1),
        0x0400_0132 => (KeyInterruptControl(), 1),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is not mapped to Gamepad memory space"),
    };

    internal int WriteByte(uint address, byte value) => throw new NotImplementedException("Write byte not implemented for gamepad registers");
    internal int WriteWord(uint address, uint value) => throw new NotImplementedException("Write word not implemented for gamepad registers");

    internal int WriteHalfWord(uint address, ushort value)
    {
        if (address == 0x0400_0132)
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
            return 1;
        }

        throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} is not mapped to Gamepad memory space");
    }

    internal void PressKey(Key key)
    {
        _keyPressed[key] = true;
    }

    internal void ReleaseKey(Key key)
    {
        _keyPressed[key] = false;
    }
}
