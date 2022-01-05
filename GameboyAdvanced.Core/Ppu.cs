namespace GameboyAdvanced.Core;

/// <summary>
/// This class both encapsulates the state of the PPU and provides 
/// functionality for rendering the current state into a bitmap
/// </summary>
internal class Ppu
{
    private readonly byte[] _paletteRam = new byte[0x400]; // 1KB
    private readonly byte[] _vram = new byte[0x18000]; // 96KB
    private readonly byte[] _oam = new byte[0x400]; // 1KB

    internal (byte, int) ReadByte(uint address) => address switch
    {
        // TODO - Cycle timing needs to include extra cycle if ppu is accessing relevant memory area on this cycle
        >= 0x0500_0000 and <= 0x0500_03FF => (_paletteRam[address & 0b0011_1111_1111], 1),
        >= 0x0600_0000 and <= 0x0601_7FFF => (_vram[address - 0x0600_0000], 1),
        >= 0x0700_0000 and <= 0x0700_03FF => (_oam[address & 0b0011_1111_1111], 1),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X4} is unused") // TODO - Handle unused addresses properly
    };

    internal void Step(int cycles)
    {
        throw new NotImplementedException();
    }

    internal (ushort, int) ReadHalfWord(uint address) => address switch
    {
        // TODO - Cycle timing needs to include extra cycle if ppu is accessing relevant memory area on this cycle
        >= 0x0500_0000 and <= 0x0500_03FF => (Utils.ReadHalfWord(_paletteRam, address, 0b0011_1111_1111), 1),
        >= 0x0600_0000 and <= 0x0601_7FFF => (Utils.ReadHalfWord(_vram, address - 0x0600_0000, 0xF_FFFF), 1),
        >= 0x0700_0000 and <= 0x0700_03FF => (Utils.ReadHalfWord(_oam, address, 0b0011_1111_1111), 1),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X4} is unused") // TODO - Handle unused addresses properly
    };

    internal (uint, int) ReadWord(uint address) => address switch
    {
        // TODO - Cycle timing needs to include extra cycle if ppu is accessing relevant memory area on this cycle
        >= 0x0500_0000 and <= 0x0500_03FF => (Utils.ReadWord(_paletteRam, address, 0b0011_1111_1111), 1),
        >= 0x0600_0000 and <= 0x0601_7FFF => (Utils.ReadWord(_vram, address - 0x0600_0000, 0xF_FFFF), 1),
        >= 0x0700_0000 and <= 0x0700_03FF => (Utils.ReadWord(_oam, address, 0b0011_1111_1111), 1),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X4} is unused") // TODO - Handle unused addresses properly
    };

    internal int WriteByte(uint address, byte value)
    {
        switch (address)
        {
            case uint _ when address is >= 0x0500_0000 and <= 0x0500_03FF:
                _paletteRam[address & 0b0011_1111_1111] = value;
                return 1;
            case uint _ when address is >= 0x0600_0000 and <= 0x0601_7FFF:
                _vram[address - 0x0600_0000] = value;
                return 1;
            case uint _ when address is >= 0x0700_0000 and <= 0x0700_03FF:
                _oam[address & 0b0011_1111_1111] = value;
                return 1;
            default:
                throw new ArgumentOutOfRangeException(nameof(address), "Address {address:X4} is unused"); // TODO - Handle unused addresses properly
        }
    }

    internal int WriteHalfWord(uint address, ushort value)
    {
        switch (address)
        {
            case uint _ when address is >= 0x0500_0000 and <= 0x0500_03FF:
                Utils.WriteHalfWord(_paletteRam, 0x3FF, address & 0x3FF, value);
                return 1;
            case uint _ when address is >= 0x0600_0000 and <= 0x0601_7FFF:
                Utils.WriteHalfWord(_vram, 0x3FF, address - 0x0600_0000, value);
                return 1;
            case uint _ when address is >= 0x0700_0000 and <= 0x0700_03FF:
                Utils.WriteHalfWord(_oam, 0x3FF, address & 0x3FF, value);
                return 1;
            default:
                throw new ArgumentOutOfRangeException(nameof(address), "Address {address:X4} is unused"); // TODO - Handle unused addresses properly
        }
    }

    internal int WriteWord(uint address, uint value)
    {
        switch (address)
        {
            case uint _ when address is >= 0x0500_0000 and <= 0x0500_03FF:
                Utils.WriteWord(_paletteRam, 0x3FF, address & 0x3FF, value);
                return 1;
            case uint _ when address is >= 0x0600_0000 and <= 0x0601_7FFF:
                Utils.WriteWord(_vram, 0x3FF, address - 0x0600_0000, value);
                return 1;
            case uint _ when address is >= 0x0700_0000 and <= 0x0700_03FF:
                Utils.WriteWord(_oam, 0x3FF, address & 0x3FF, value);
                return 1;
            default:
                throw new ArgumentOutOfRangeException(nameof(address), "Address {address:X4} is unused"); // TODO - Handle unused addresses properly
        }
    }
}
