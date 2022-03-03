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

    /// <summary>
    /// SRAM is only accessible over an 8 bit bus
    /// </summary>
    internal byte ReadSRam(uint address)
    {
        return _sram[address & 0x0EFF_FFFF & 0x7FFF];
    }

    internal void WriteSRam(uint address, byte value)
    {
        _sram[address & 0x0EFF_FFFF & 0x7FFF] = value;
    }

    internal byte ReadByte(uint address) => address < _data.Length ? _data[address] : (byte)(address >> 1 >> (int)((address & 1) * 8));

    internal ushort ReadHalfWord(uint address) => address < _data.Length ? Utils.ReadHalfWord(_data, address, 0x1FF_FFFF) : (ushort)(address >> 1);

    internal uint ReadWord(uint address) => address < _data.Length 
        ? Utils.ReadWord(_data, address, 0x1FF_FFFF) 
        : (((address & 0xFFFF_FFFC) >> 1) & 0xFFFF) | (((((address & 0xFFFF_FFFC) + 2) >> 1) & 0xFFFF) << 16);
}
