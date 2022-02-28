using System.Text;

namespace GameboyAdvanced.Core.Rom;

public class GamePak
{
    private readonly byte[] _header = new byte[0xC0];
    private readonly byte[] _data = new byte[0x200_0000];
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
        Array.Copy(bin, 0, _data, 0, Math.Min(bin.Length, _data.Length));

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

    /// <summary>
    /// SRAM is only accessible over an 8 bit bus
    /// </summary>
    internal byte ReadSRam(uint address)
    {
        return _sram[address & 0xFFFF];
    }

    internal void WriteSRam(uint address, byte value)
    {
        _sram[address & 0xFFFF] = value;
    }

    internal byte ReadByte(uint address) => _data[address & 0x1FF_FFFF];

    internal ushort ReadHalfWord(uint address) => Utils.ReadHalfWord(_data, address, 0x1FF_FFFF);

    internal uint ReadWord(uint address) => Utils.ReadWord(_data, address, 0x1FF_FFFF);
}
