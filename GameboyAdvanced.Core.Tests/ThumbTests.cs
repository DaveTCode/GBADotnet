using GameboyAdvanced.Core.Cpu;
using Xunit;

namespace GameboyAdvanced.Core.Tests;

public class ThumbTests
{
    private readonly static byte[] _bios = new byte[0x4000];

    [Theory]
    [InlineData(0, 0, 0, false)]
    [InlineData(1, 0, 1, false)]
    [InlineData(1, 1, 2, false)]
    [InlineData(0x8000_0000, 1, 0, true)]
    [InlineData(0x8000_0000, 2, 0, false)]
    public void TestLSL(uint rs, int offset, uint expected, bool expectedCarry)
    {
        var ppu = new Ppu();
        var bus = new MemoryBus(_bios, ppu);
        var cpu = new Arm7Tdmi(bus);
        cpu.R[1] = rs;

        _ = Thumb.LSL_Shift_Reg(cpu, (ushort)(((offset & 0b1_1111) << 6) | 0b00_1000));

        Assert.Equal(expected, cpu.R[0]);
        Assert.Equal(expectedCarry, cpu.Cpsr.CarryFlag);
    }

    [Theory]
    [InlineData(0, 0, 0, false)]
    [InlineData(1, 0, 0, false)] // LSR#0 -> LSR#32
    [InlineData(1, 1, 0, true)]
    [InlineData(0x8000_0000, 1, 0x4000_0000, false)]
    public void TestLSR(uint rs, int offset, uint expected, bool expectedCarry)
    {
        var ppu = new Ppu();
        var bus = new MemoryBus(_bios, ppu);
        var cpu = new Arm7Tdmi(bus);
        cpu.R[1] = rs;

        _ = Thumb.LSR_Shift_Reg(cpu, (ushort)(((offset & 0b1_1111) << 6) | 0b1000_0000_1000));

        Assert.Equal(expected, cpu.R[0]);
        Assert.Equal(expectedCarry, cpu.Cpsr.CarryFlag);
    }

    [Theory]
    [InlineData(0, 0, 0, false)]
    [InlineData(1, 0, 0, false)] // ASR#0 -> ASR#32
    [InlineData(1, 1, 0, true)]
    [InlineData(0x8000_0000, 1, 0xC000_0000, false)] // Retain bit 31
    public void TestASR(uint rs, int offset, uint expected, bool expectedCarry)
    {
        var ppu = new Ppu();
        var bus = new MemoryBus(_bios, ppu);
        var cpu = new Arm7Tdmi(bus);
        cpu.R[1] = rs;

        _ = Thumb.ASR_Shift_Reg(cpu, (ushort)(((offset & 0b1_1111) << 6) | 0b1_0000_0000_1000));

        Assert.Equal(expected, cpu.R[0]);
        Assert.Equal(expectedCarry, cpu.Cpsr.CarryFlag);
    }
}
