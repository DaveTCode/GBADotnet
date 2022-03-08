namespace GameboyAdvanced.Core.Ppu;

internal partial class Ppu
{
    /// <summary>
    /// Palette entries correspond to a single 16 bit entry in the palette and are
    /// pre-computed to cache the values and reduce costs of rendering.
    /// </summary>
    internal struct PaletteEntry
    {
        internal byte R;
        internal byte G;
        internal byte B;

        internal void Set(ushort value)
        {
            R = (byte)((value & 0b11111) << 3);
            G = (byte)(((value >> 5) & 0b11111) << 3);
            B = (byte)(((value >> 10) & 0b11111) << 3);
        }

        public override string ToString() => $"R{R},G{G},B{B}";
    }

    /// <summary>
    /// Palette ram is only accessible over a 16 bit bus and each palette entry
    /// is a 2 byte wide value
    /// </summary>
    private readonly ushort[] _paletteRam = new ushort[0x200]; // 1KB

    private readonly PaletteEntry[] _paletteEntries = new PaletteEntry[0x200];

    /// <summary>
    /// The backdrop color is the first color in palette zero and is used
    /// when no other color would be displayed due to transparency.
    /// 
    /// We cache it here for a minor performance benefit.
    /// </summary>
    internal PaletteEntry BackdropColor;

    /// <summary>
    /// Since internally palette memory is stored in 16 bit sections
    /// writing a byte actually writes that byte to both bytes of the
    /// half word aligned area in memory.
    /// </summary>
    internal void WritePaletteByte(uint address, byte value) => 
        WritePaletteHalfWord(address & 0xFFFF_FFFE, (ushort)((value << 8) | value));

    internal void WritePaletteHalfWord(uint address, ushort value)
    {
        var paletteIx = (address & 0x3FF) >> 1;
        _paletteRam[paletteIx] = value;

        _paletteEntries[paletteIx].Set(value);

        if (paletteIx == 0)
        {
            BackdropColor = _paletteEntries[0];
        }
    }

    /// <summary>
    /// Reading a byte from palette ram is possible and returns the aligned 
    /// byte as one would expect.
    /// </summary>
    internal byte ReadPaletteByte(uint address) =>
        (byte)(ReadPaletteHalfWord(address) >> (int)(8 * (address & 0b1)));

    internal ushort ReadPaletteHalfWord(uint address) => _paletteRam[(address & 0x3FF) >> 1];
}
