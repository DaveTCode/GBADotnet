using System.Text;

namespace GameboyAdvanced.Core.Rom;

public class GamePak
{
    private readonly byte[] _header = new byte[0xC0];
    private readonly byte[] _data;
    private readonly byte[] _sram = new byte[0x1_0000];

    public readonly uint RomEntryPoint;
    public readonly byte[] LogoCompressed = new byte[156];
    public readonly string GameTitle;
    public readonly string GameCode;
    public readonly string MakerCode;
    public readonly byte FixedValue;
    public readonly byte MainUnitCode;
    public readonly byte DeviceType;
    public readonly byte[] ReservedArea1 = new byte[7];
    public readonly byte SoftwareVersion;
    public readonly byte ComplementCheck;
    public readonly byte[] ReservedArea2 = new byte[2];

    // TODO - Multiboot details

    public GamePak(byte[] bin)
    {
        if (bin == null) throw new ArgumentNullException(nameof(bin));
        if (bin.LongLength < 0xC0) throw new ArgumentException("Rom must be >= 0xC0 in size to fit cartridge header", nameof(bin));
        Array.Copy(bin, 0, _header, 0, _header.Length);

        _data = (byte[])bin.Clone();

        RomEntryPoint = Utils.ReadWord(_header, 0, 0xFFFF_FFFF);
        Array.Copy(_header, 4, LogoCompressed, 0, LogoCompressed.Length);
        GameTitle = Encoding.ASCII.GetString(_header[160..171].TakeWhile(b => b != 0).ToArray());
        GameCode = Encoding.ASCII.GetString(_header[172..175]);
        MakerCode = Encoding.ASCII.GetString(_header[176..177]);
        FixedValue = _header[178];
        MainUnitCode = _header[179];
        DeviceType = _header[180];
        Array.Copy(_header, 181, ReservedArea1, 0, ReservedArea1.Length);
        SoftwareVersion = _header[188];
        ComplementCheck = _header[189];
        Array.Copy(_header, 190, ReservedArea2, 0, ReservedArea2.Length);
    }

    internal (byte, int) ReadByte(uint address)
    {
        if ((address & 0x0E00_0000) == 0x0E00_0000)
        {
            return (_sram[address & 0xFFFF], 4);
        }
        else
        {
            return (_data[address & 0x1FF_FFFF], 4); // TODO - Not really 4 waits, depends on N/S access and doesn't take into account the configured wait states
        }
    }

    internal (ushort, int) ReadHalfWord(uint address)
    {
        if ((address & 0x0E00_0000) == 0x0E00_0000)
        {
            return (_sram[address & 0xFFFF], 4); // TODO - data bus is 8 bit wide, suspect this means it returns a byte
        }
        else
        {
            return (Utils.ReadHalfWord(_data, address & 0x1FF_FFFF, 0x1FF_FFFF), 4); // TODO - Not really 4 waits, depends on N/S access and doesn't take into account the configured wait states
        }
    }

    internal (uint, int) ReadWord(uint address)
    {
        if ((address & 0x0E00_0000) == 0x0E00_0000)
        {
            return (_sram[address & 0xFFFF], 7); // TODO - data bus is 8 bit wide, suspect this means it returns a byte
        }
        else
        {
            return (Utils.ReadWord(_data, address & 0x1FF_FFFF, 0x1FF_FFFF), 7); // TODO - Not really 7 waits, depends on N/S access and doesn't take into account the configured wait states
        }
    }

    internal int WriteByte(uint address, byte value)
    {
        if ((address & 0x0E00_0000) == 0x0E00_0000)
        {
            _sram[address & 0xFFFF] = value;
            return 4;
        }

        return 0; // TODO - Can't write to GamePak rom but how many wait states are injected?
    }

    internal int WriteHalfWord(uint address, ushort value)
    {
        if ((address & 0x0E00_0000) == 0x0E00_0000)
        {
            _sram[address & 0xFFFF] = (byte)value; // TODO - 8 bit bus to SRAM, presumably that means that only 8 bits are written from the half word
            return 4;
        }

        return 0; // TODO - Can't write to GamePak rom but how many wait states are injected?
    }

    internal int WriteWord(uint address, uint value)
    {
        if ((address & 0x0E00_0000) == 0x0E00_0000)
        {
            _sram[address & 0xFFFF] = (byte)value; // TODO - 8 bit bus to SRAM, presumably that means that only 8 bits are written from the word
            return 5;
        }

        return 1; // TODO - Can't write to GamePak rom but how many cycles are taken trying?
    }
}
