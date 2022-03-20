using System.Runtime.CompilerServices;

namespace GameboyAdvanced.Core.Ppu;

public struct AffineTransform
{
    internal short PA;
    internal short PB;
    internal short PC;
    internal short PD;

    internal void Reset()
    {
        PA = PB = PC = PD = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void TransformVector(ref int x, ref int y)
    {
        var newX = ((x * PA) + (y * PB)) >> 8;
        y = ((x * PC) + (y * PD)) >> 8;
        x = newX;
    }

    public override string ToString() => $"{PA},{PB},{PC},{PD}";
}

public partial class Ppu
{
    private readonly ushort[] _oam = new ushort[0x200]; // 1KB
    private readonly Sprite[] _sprites = new Sprite[128]; // OBJ0-127
    private readonly AffineTransform[] _spriteAffineTransforms = new AffineTransform[32];

    internal byte ReadOamByte(uint address) => 
        (byte)(ReadOamHalfWord(address) >> (int)(8 * (address & 0b1)));

    internal ushort ReadOamHalfWord(uint address)
    {
        return _oam[(address & 0x3FF) >> 1];
    }

    internal void WriteOamHalfWord(uint address, ushort value)
    {
        var maskedAddress = address & 0x3FF;
        _oam[maskedAddress >> 1] = value;

        switch ((maskedAddress >> 1) & 0b11)
        {
            case 0:
                _sprites[maskedAddress >> 3].UpdateAttr1(value);
                break;
            case 1:
                _sprites[maskedAddress >> 3].UpdateAttr2(value);
                break;
            case 2:
                _sprites[maskedAddress >> 3].UpdateAttr3(value);
                break;
            case 3:
                var group = (maskedAddress & 0xFFFF) / 0x20;
                var groupItem = maskedAddress % 0x20;

                switch (groupItem)
                {
                    case 0x06:
                        _spriteAffineTransforms[group].PA = (short)value;
                        break;
                    case 0x0E:
                        _spriteAffineTransforms[group].PB = (short)value;
                        break;
                    case 0x16:
                        _spriteAffineTransforms[group].PC = (short)value;
                        break;
                    case 0x1E:
                        _spriteAffineTransforms[group].PD = (short)value;
                        break;
                }

                break;
            default:
                throw new Exception("Invalid masked address for oam half word write");
        }
    }
}
