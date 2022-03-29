using GameboyAdvanced.Core.Dma;
using System.Text;
using System.Text.RegularExpressions;

namespace GameboyAdvanced.Core.Rom;

public enum RomBackupType
{
    EEPROM,
    SRAM,
    FLASH64,
    FLASH128,
}

public class GamePak
{
    private readonly byte[] _header = new byte[0xC0];
    public readonly byte[] Data;
    private readonly byte[] _sram = new byte[0x1_0000];
    private readonly FlashBackup? _flashBackup;
    private readonly EEPromBackup? _eepromBackup;

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
    public readonly RomBackupType RomBackupType;

    // TODO - Multiboot details

    public GamePak(byte[] bin, RomBackupType? romBackupType = null)
    {
        if (bin == null) throw new ArgumentNullException(nameof(bin));
        if (bin.LongLength < 0xC0) throw new ArgumentException("Rom must be >= 0xC0 in size to fit cartridge header", nameof(bin));
        Array.Copy(bin, 0, _header, 0, _header.Length);
        Data = (byte[])bin.Clone();

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

        RomBackupType = romBackupType ?? CalculateRomBackupType(bin);

        if (RomBackupType == RomBackupType.FLASH128)
        {
            _flashBackup = new FlashBackup(0x62, 0x13); // Sanyo
        }
        else if (RomBackupType == RomBackupType.FLASH64)
        {
            _flashBackup = new FlashBackup(0xBF, 0xD4); // SST
        }
        else if (RomBackupType == RomBackupType.EEPROM)
        {
            var mask = Data.Length > 0x0100_0000 ? 0x01FF_FF00u : 0x0100_0000u;

            _eepromBackup = new EEPromBackup(mask);
        }
    }

    internal void SetDmaDataUnit(DmaDataUnit dma)
    {
        if (_eepromBackup != null)
        {
            _eepromBackup.DmaDataUnit = dma;
        }
    }

    private static RomBackupType CalculateRomBackupType(byte[] bin)
    {
        var romAsString = Encoding.ASCII.GetString(bin);

        if (Regex.IsMatch(romAsString, @"EEPROM_V\d\d\d"))
        {
            return RomBackupType.EEPROM;
        }
        else if (Regex.IsMatch(romAsString, @"SRAM_V\d\d\d"))
        {
            return RomBackupType.SRAM;
        }
        else if (Regex.IsMatch(romAsString, @"FLASH(512)?_V\d\d\d"))
        {
            return RomBackupType.FLASH64;
        }
        else if (Regex.IsMatch(romAsString, @"FLASH1M_V\d\d\d"))
        {
            return RomBackupType.FLASH128;
        }

        return RomBackupType.SRAM;
    }

    /// <summary>
    /// Backup storage is only available over an 8 bit bus and behaves very 
    /// differently depending on what type of storage is used on the cart.
    /// </summary>
    /// 
    /// <remarks>
    /// TODO - Potentially improve performance here if it turns out 
    /// to be hit a lot by predeciding which function will be correct
    /// using delegate* at construction
    /// </remarks>
    internal byte ReadBackupStorage(uint address) => RomBackupType switch
    {
        RomBackupType.SRAM => _sram[address & 0x0EFF_FFFF & 0x7FFF],
        RomBackupType.FLASH64 => _flashBackup!.Read(address),
        RomBackupType.FLASH128 => _flashBackup!.Read(address),
        RomBackupType.EEPROM => _eepromBackup!.Read(address),
        _ => throw new Exception($"Invalid backup storage type {RomBackupType}")
    };

    internal void WriteBackupStorage(uint address, byte value)
    {
        switch (RomBackupType)
        {
            case RomBackupType.SRAM:
                _sram[address & 0x0EFF_FFFF & 0x7FFF] = value;
                break;
            case RomBackupType.FLASH128:
            case RomBackupType.FLASH64:
                _flashBackup!.Write(address, value);
                break;
            case RomBackupType.EEPROM:
                break;
        }
    }

    internal void Write(uint address, byte value)
    {
        if (RomBackupType == RomBackupType.EEPROM && _eepromBackup!.IsEEPromAddress(address))
        {
            _eepromBackup!.Write(address, value);
        }
    }

    internal byte ReadByte(uint address)
    {
        if (_eepromBackup != null && _eepromBackup.IsEEPromAddress(address))
        {
            return _eepromBackup.Read(address);
        }

        return address < Data.Length ?
            Data[address] :
            (byte)(address >> 1 >> (int)((address & 1) * 8));
    }

    internal ushort ReadHalfWord(uint address)
    {
        if (_eepromBackup != null && _eepromBackup.IsEEPromAddress(address))
        {
            return _eepromBackup.Read(address);
        }

        return address < Data.Length
            ? Utils.ReadHalfWord(Data, address, 0x1FF_FFFF)
            : (ushort)(address >> 1);
    }

    internal uint ReadWord(uint address)
    {
        if (_eepromBackup != null && _eepromBackup.IsEEPromAddress(address))
        {
            return _eepromBackup.Read(address);
        }

        return address < Data.Length
            ? Utils.ReadWord(Data, address, 0x1FF_FFFF)
            : (((address & 0xFFFF_FFFC) >> 1) & 0xFFFF) | (((((address & 0xFFFF_FFFC) + 2) >> 1) & 0xFFFF) << 16);
    }

    public override string ToString()
    {
        return $"{GameTitle} - {RomBackupType}";
    }
}
