namespace GameboyAdvanced.Core.Ppu;

internal struct SpriteRotation
{
    internal int PA;
    internal int PB;
    internal int PC;
    internal int PD;

    internal void Reset()
    {
        PA = PB = PC = PD = 0;
    }

    public override string ToString() => $"{PA},{PB},{PC},{PD}";
}

internal partial class Ppu
{
    private readonly ushort[] _oam = new ushort[0x200]; // 1KB
    private readonly Sprite[] _sprites = new Sprite[128]; // OBJ0-127
    private readonly SpriteRotation[] _spriteRotations = new SpriteRotation[32];

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
                        _spriteRotations[group].PA = value;
                        break;
                    case 0x0E:
                        _spriteRotations[group].PB = value;
                        break;
                    case 0x16:
                        _spriteRotations[group].PC = value;
                        break;
                    case 0x1E:
                        _spriteRotations[group].PD = value;
                        break;
                }

                break;
            default:
                throw new Exception("Invalid masked address for oam half word write");
        }
    }
}
