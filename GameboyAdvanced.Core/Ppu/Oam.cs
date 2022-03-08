namespace GameboyAdvanced.Core.Ppu;

internal partial class Ppu
{
    private readonly ushort[] _oam = new ushort[0x200]; // 1KB
    private readonly Sprite[] _sprites = new Sprite[128]; // OBJ0-127

    internal byte ReadOamByte(uint address) => 
        (byte)(ReadOamHalfWord(address) >> (int)(8 * (address & 0b1)));

    internal ushort ReadOamHalfWord(uint address)
    {
        return _oam[(address & 0x3FF) >> 1];
    }

    internal void WriteOamHalfWord(uint address, ushort value)
    {
        _oam[(address & 0x3FF) >> 1] = value;
    }
}
