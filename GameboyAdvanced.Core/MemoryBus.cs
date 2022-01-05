namespace GameboyAdvanced.Core;

internal class MemoryBus
{
    private readonly Ppu _ppu;
    private readonly byte[] _bios = new byte[0x4000];
    private readonly byte[] _onBoardWRam = new byte[0x4_0000];
    private readonly byte[] _onChipWRam = new byte[0x8000];

    internal MemoryBus(byte[] bios, Ppu ppu)
    {
        if (bios == null || bios.Length > _bios.Length) throw new ArgumentException($"Bios is invalid length {bios?.Length}", nameof(bios));
        Array.Fill<byte>(_bios, 0);
        Array.Copy(bios, 0, _bios, 0, Math.Min(_bios.Length, bios.Length));

        _ppu = ppu ?? throw new ArgumentNullException(nameof(ppu));
    }

    internal (byte, int) ReadByte(uint address) => address switch
    {
        uint a when a <= 0x0000_3FFF => (_bios[a], 1), // TODO - Can only read from bios when IP is located in BIOS region
        uint a when a is >= 0x0200_0000 and <= 0x0203_FFFF => (_onBoardWRam[a & 0x3_FFFF], 3),
        uint a when a is >= 0x0300_0000 and <= 0x0300_7FFF => (_onChipWRam[a & 0x7FFF], 1),
        uint a when a is >= 0x0400_0000 and <= 0x0400_03FE => throw new NotImplementedException("IO registers not implemented"),
        uint a when a is >= 0x0500_0000 and <= 0x07FF_FFFF => _ppu.ReadByte(address),
        uint a when a is >= 0x0800_0000 and <= 0x0FFF_FFFF => throw new NotImplementedException("GamePAK not yet implemented"),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} not memory mapped")
    };

    internal (ushort, int) ReadHalfWord(uint address) => address switch
    {
        uint a when a <= 0x0000_3FFF => (Utils.ReadHalfWord(_bios, address, 0x3FFF), 1), // TODO - Can only read from bios when IP is located in BIOS region
        uint a when a is >= 0x0200_0000 and <= 0x0203_FFFF => (Utils.ReadHalfWord(_onBoardWRam, address, 0x3_FFFF), 3),
        uint a when a is >= 0x0300_0000 and <= 0x0300_7FFF => (Utils.ReadHalfWord(_onChipWRam, address, 0x7FFF), 1),
        uint a when a is >= 0x0400_0000 and <= 0x0400_03FE => throw new NotImplementedException("IO registers not implemented"),
        uint a when a is >= 0x0500_0000 and <= 0x07FF_FFFF => _ppu.ReadHalfWord(address),
        uint a when a is >= 0x0800_0000 and <= 0x0FFF_FFFF => throw new NotImplementedException("GamePAK not yet implemented"),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} not memory mapped")
    };

    internal (uint, int) ReadWord(uint address) => address switch
    {
        uint a when a <= 0x0000_3FFF => (Utils.ReadWord(_bios, address, 0x3FFF), 1), // TODO - Can only read from bios when IP is located in BIOS region
        uint a when a is >= 0x0200_0000 and <= 0x0203_FFFF => (Utils.ReadWord(_onBoardWRam, address, 0x3_FFFF), 6),
        uint a when a is >= 0x0300_0000 and <= 0x0300_7FFF => (Utils.ReadWord(_onChipWRam, address, 0x7FFF), 1),
        uint a when a is >= 0x0400_0000 and <= 0x0400_03FE => throw new NotImplementedException("IO registers not implemented"),
        uint a when a is >= 0x0500_0000 and <= 0x07FF_FFFF => _ppu.ReadWord(address),
        uint a when a is >= 0x0800_0000 and <= 0x0FFF_FFFF => throw new NotImplementedException("GamePAK not yet implemented"),
        _ => throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X8} not memory mapped")
    };

    internal int WriteByte(uint address, byte value)
    {
        switch (address)
        {
            case uint _ when address <= 0x0000_3FFF:
                return 1;
            case uint _ when address is >= 0x0200_0000 and <= 0x0203_FFFF:
                _onBoardWRam[address & 0x3_FFFF] = value;
                return 3;
            case uint _ when address is >= 0x0300_0000 and <= 0x0300_7FFF:
                _onChipWRam[address & 0x7FFF] = value;
                return 1;
            case uint _ when address is >= 0x0400_0000 and <= 0x0400_03FE:
                throw new NotImplementedException("IO registers not implemented");
            case uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF:
                return _ppu.WriteByte(address, value);
            case uint _ when address is >= 0x0800_0000 and <= 0x0FFF_FFFF:
                throw new NotImplementedException("GamePAK not yet implemented");
            default:
                throw new ArgumentOutOfRangeException(nameof(address));
        }
    }

    internal int WriteHalfWord(uint address, ushort value)
    {
        switch (address)
        {
            case uint _ when address <= 0x0000_3FFF:
                return 1;
            case uint _ when address is >= 0x0200_0000 and <= 0x0203_FFFF:
                Utils.WriteHalfWord(_onBoardWRam, 0x3_FFFF, address & 0x3_FFFF, value);
                return 3;
            case uint _ when address is >= 0x0300_0000 and <= 0x0300_7FFF:
                Utils.WriteHalfWord(_onChipWRam, 0x7FFF, address & 0x7FFF, value);
                return 1;
            case uint _ when address is >= 0x0400_0000 and <= 0x0400_03FE:
                throw new NotImplementedException("IO registers not implemented");
            case uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF:
                return _ppu.WriteHalfWord(address, value);
            case uint _ when address is >= 0x0800_0000 and <= 0x0FFF_FFFF:
                throw new NotImplementedException("GamePAK not yet implemented");
            default:
                throw new ArgumentOutOfRangeException(nameof(address));
        }
    }

    internal int WriteWord(uint address, uint value)
    {
        switch (address)
        {
            case uint _ when address <= 0x0000_3FFF:
                return 1;
            case uint _ when address is >= 0x0200_0000 and <= 0x0203_FFFF:
                Utils.WriteWord(_onBoardWRam, 0x3_FFFF, address & 0x3_FFFF, value);
                return 3;
            case uint _ when address is >= 0x0300_0000 and <= 0x0300_7FFF:
                Utils.WriteWord(_onChipWRam, 0x7FFF, address & 0x7FFF, value);
                return 1;
            case uint _ when address is >= 0x0400_0000 and <= 0x0400_03FE:
                throw new NotImplementedException("IO registers not implemented");
            case uint _ when address is >= 0x0500_0000 and <= 0x07FF_FFFF:
                return _ppu.WriteWord(address, value);
            case uint _ when address is >= 0x0800_0000 and <= 0x0FFF_FFFF:
                throw new NotImplementedException("GamePAK not yet implemented");
            default:
                throw new ArgumentOutOfRangeException(nameof(address));
        }
    }
}