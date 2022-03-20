namespace GameboyAdvanced.Core.Ppu.Registers;

public struct BldAlpha
{
    private ushort _raw;
    internal int EVACoefficient;
    internal int EVBCoefficient;

    internal void Reset()
    {
        _raw = 0;
        EVACoefficient = 0;
        EVBCoefficient = 0;
    }

    internal void UpdateB1(byte val)
    {
        _raw = (ushort)((_raw & 0xFFFF_0000) | val);
        _raw &= 0b0001_1111_0001_1111;
        var evaBase = val & 0b1_1111;
        EVACoefficient = evaBase >= 16 ? 16 : evaBase;
    }

    internal void UpdateB2(byte val)
    {
        _raw = (ushort)((_raw & 0x0000_FFFF) | (ushort)(val << 8));
        _raw &= 0b0001_1111_0001_1111;
        var evbBase = val & 0b1_1111;
        EVBCoefficient = evbBase >= 16 ? 16 : evbBase;
    }

    internal ushort Get() => _raw;
}
