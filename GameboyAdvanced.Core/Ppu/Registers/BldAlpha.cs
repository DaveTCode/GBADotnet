namespace GameboyAdvanced.Core.Ppu.Registers;

internal struct BldAlpha
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

    internal void Set(ushort val)
    {
        _raw = (ushort)(val & 0b0001_1111_0001_1111);
        var evaBase = val & 0b1_1111;
        EVACoefficient = evaBase >= 16 ? 16 : evaBase;
        var evbBase = (val >> 8) & 0b1_1111;
        EVBCoefficient = evbBase >= 16 ? 16 : evbBase;
    }

    internal ushort Get() => _raw;
}
