using System.Runtime.CompilerServices;

namespace GameboyAdvanced.Core.Ppu;

public enum SpecialEffect
{
    None = 0b00,
    AlphaBlend = 0b01,
    IncreaseBrightness = 0b10,
    DecreaseBrightness = 0b11,
}

/// <summary>
/// The GBA supports two types of special effects controlled by 3 registers.
/// 
/// 1. Alpha blending of two layers
/// 2. Brightness increase/decrease
/// 
/// Really they're the same thing just the second layer is white/black in the 
/// brightness case.
/// 
/// This class encapsulates all the data for blending but the actual rendering
/// is handled by the <see cref="ScanlineRenderer"/>
/// </summary>
public class ColorEffects
{
    public bool[][] TargetBg = new bool[2][]
    {
        new bool[4], new bool[4],
    };
    public bool[] TargetObj = new bool[2];
    public bool[] TargetBackdrop = new bool[2];

    public SpecialEffect SpecialEffect;

    private ushort _bldAlphaRaw;
    public int EVACoefficient;
    public int EVBCoefficient;
    public int EVYCoefficient;

    internal void Reset()
    {
        for (var target = 0; target < 2; target++)
        {
            Array.Clear(TargetBg[target]);
            TargetObj[target] = false;
            TargetBackdrop[target] = false;
        }
        SpecialEffect = SpecialEffect.None;

        _bldAlphaRaw = 0;
        EVACoefficient = 0;
        EVBCoefficient = 0;
        EVYCoefficient = 0;
    }

    internal ushort BldCnt() => (ushort)((TargetBg[0][0] ? 1 << 0 : 0)
            | (TargetBg[0][1] ? 1 << 1 : 0)
            | (TargetBg[0][2] ? 1 << 2 : 0)
            | (TargetBg[0][3] ? 1 << 3 : 0)
            | (TargetObj[0] ? 1 << 4 : 0)
            | (TargetBackdrop[0] ? 1 << 5 : 0)
            | ((ushort)SpecialEffect << 6)
            | (TargetBg[1][0] ? 1 << 8 : 0)
            | (TargetBg[1][1] ? 1 << 9 : 0)
            | (TargetBg[1][2] ? 1 << 10 : 0)
            | (TargetBg[1][3] ? 1 << 11 : 0)
            | (TargetObj[1] ? 1 << 12 : 0)
            | (TargetBackdrop[1] ? 1 << 13 : 0));

    internal void UpdateBldCntB1(byte val)
    {
        TargetBg[0][0] = (val & (1 << 0)) != 0;
        TargetBg[0][1] = (val & (1 << 1)) != 0;
        TargetBg[0][2] = (val & (1 << 2)) != 0;
        TargetBg[0][3] = (val & (1 << 3)) != 0;
        TargetObj[0] = (val & (1 << 4)) != 0;
        TargetBackdrop[0] = (val & (1 << 5)) != 0;
        SpecialEffect = (SpecialEffect)((val >> 6) & 0b11);
    }

    internal void UpdateBldCntB2(byte val)
    {
        TargetBg[1][0] = (val & (1 << 0)) != 0;
        TargetBg[1][1] = (val & (1 << 1)) != 0;
        TargetBg[1][2] = (val & (1 << 2)) != 0;
        TargetBg[1][3] = (val & (1 << 3)) != 0;
        TargetObj[1] = (val & (1 << 4)) != 0;
        TargetBackdrop[1] = (val & (1 << 5)) != 0;
    }

    internal ushort BldAlpha() => _bldAlphaRaw;

    internal void UpdateBldAlphaB1(byte val)
    {
        _bldAlphaRaw = (ushort)((_bldAlphaRaw & 0xFFFF_0000) | val);
        _bldAlphaRaw &= 0b0001_1111_0001_1111;
        var evaBase = val & 0b1_1111;
        EVACoefficient = evaBase >= 16 ? 16 : evaBase;
    }

    internal void UpdateBldAlphaB2(byte val)
    {
        _bldAlphaRaw = (ushort)((_bldAlphaRaw & 0x0000_FFFF) | (ushort)(val << 8));
        _bldAlphaRaw &= 0b0001_1111_0001_1111;
        var evbBase = val & 0b1_1111;
        EVBCoefficient = evbBase >= 16 ? 16 : evbBase;
    }

    internal void UpdateBldy(byte val)
    {
        var evyBase = val & 0b1_1111;
        EVYCoefficient = evyBase >= 16 ? 16 : evyBase;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte AlphaIntensity(byte intensityPixel1, byte intensityPixel2) =>
        (byte)Math.Min(248, ((byte)(((intensityPixel1 >> 3) * EVACoefficient / 16f) + ((intensityPixel2 >> 3) * EVBCoefficient / 16f)) << 3));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte BrightnessIncrease(byte intensity) =>
        (byte)(((intensity >> 3) + (byte)((31 - (intensity >> 3)) * (EVYCoefficient / 16f))) << 3);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte BrightnessDecrease(byte intensity) =>
        (byte)(((intensity >> 3) - (byte)((intensity >> 3) * (EVYCoefficient / 16f))) << 3);
}
