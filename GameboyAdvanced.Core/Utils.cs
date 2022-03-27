using System.Runtime.CompilerServices;
using static GameboyAdvanced.Core.Ppu.Ppu;

namespace GameboyAdvanced.Core;

public static class Utils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadHalfWord(byte[] data, uint address, uint mask) =>
        (ushort)(data[address & mask] | (data[(address + 1) & mask] << 8));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadWord(byte[] data, uint address, uint mask) =>
        (uint)(data[address & mask] | (data[(address + 1) & mask] << 8) | (data[(address + 2) & mask] << 16) | (data[(address + 3) & mask] << 24));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteHalfWord(byte[] data, uint mask, uint address, ushort value)
    {
        data[address & mask] = (byte)value;
        data[(address + 1) & mask] = (byte)(value >> 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteWord(byte[] data, uint mask, uint address, uint value)
    {
        data[address & mask] = (byte)value;
        data[(address + 1) & mask] = (byte)(value >> 8);
        data[(address + 2) & mask] = (byte)(value >> 16);
        data[(address + 3) & mask] = (byte)(value >> 24);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FastWriteWord(byte[] data, uint mask, uint address, uint value)
    {
        data[address & mask] = (byte)value;
        data[(address + 1) & mask] = (byte)(value >> 8);
        data[(address + 2) & mask] = (byte)(value >> 16);
        data[(address + 3) & mask] = (byte)(value >> 24);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ColorToRgb(PaletteEntry color, Span<byte> buffer)
    {
        buffer[0] = color.R;
        buffer[1] = color.G;
        buffer[2] = color.B;
        buffer[3] = 0xFF; // Alpha channel
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ColorToRgb(int color, Span<byte> buffer)
    {
        buffer[0] = (byte)((color & 0b11111) << 3);
        buffer[1] = (byte)(((color >> 5) & 0b11111) << 3);
        buffer[2] = (byte)(((color >> 10) & 0b11111) << 3);
        buffer[3] = 0xFF; // Alpha channel
    }
}
