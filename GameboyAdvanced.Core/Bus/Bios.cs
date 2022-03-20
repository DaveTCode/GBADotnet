using System.Runtime.CompilerServices;

namespace GameboyAdvanced.Core.Bus;

public class Bios
{
    private readonly byte[] _bios = new byte[0x4000];

    /// <summary>
    /// Bios reads when R15>=0x4000 don't go through normal open bus but 
    /// instead take the most recently latched value from the BIOS area.
    /// 
    /// No idea how that works on hardware though, is there a dedicated 
    /// line for bios?
    /// </summary>
    private uint _latchedValue;

    internal Bios(byte[] bios, bool skipBios)
    {
        if (bios == null || bios.Length > _bios.Length) throw new ArgumentException($"Bios is invalid length {bios?.Length}", nameof(bios));
        Array.Fill<byte>(_bios, 0);
        Array.Copy(bios, 0, _bios, 0, Math.Min(_bios.Length, bios.Length));

        // When we're skpping the bios we need to set up the initial latch value
        // for bios open bus
        if (skipBios)
        {
            _latchedValue = 0xE129F000;
        }
    }

    internal void Reset(bool skipBios)
    {
        _latchedValue = skipBios ? 0xE129F000 : 0x0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte ReadByte(uint address, uint r15)
    {
        if (r15 <= 0x3FFF)
        {
            _latchedValue = _bios[address];
            return (byte)_latchedValue;
        }
        
        var rotate = (address & 0b11) * 8;
        return (byte)(_latchedValue >> (int)rotate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ushort ReadHalfWord(uint address, uint r15)
    {
        if (r15 <= 0x3FFF)
        {
            _latchedValue = Utils.ReadHalfWord(_bios, address & 0x3FFE, 0x3FFF);
            return (ushort)_latchedValue;
        }

        if ((address & 0b11) > 1)
        {
            return (ushort)((_latchedValue >> 16));
        }

        return (ushort)_latchedValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal uint ReadWord(uint address, uint r15)
    {
        if (r15 <= 0x3FFF)
        {
            _latchedValue = Utils.ReadWord(_bios, address & 0x3FFC, 0x3FFF);
            return _latchedValue;
        }

        return _latchedValue;
    }
}
