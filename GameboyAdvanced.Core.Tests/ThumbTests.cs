using GameboyAdvanced.Core.Cpu;
using GameboyAdvanced.Core.Cpu.Interrupts;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Dma;
using GameboyAdvanced.Core.Input;
using GameboyAdvanced.Core.Rom;
using GameboyAdvanced.Core.Serial;
using GameboyAdvanced.Core.Timer;
using Xunit;

namespace GameboyAdvanced.Core.Tests;

public class ThumbTests
{
    private readonly static byte[] _bios = new byte[0x4000];
    private readonly static Ppu.Ppu _testPpu = new();
    private readonly static GamePak _testGamePak = new(new byte[0xFF_FFFF]);
    private readonly static Gamepad _testGamepad = new();
    private readonly static DmaController _testDmaController = new();
    private readonly static TimerController _testTimerController = new();
    private readonly static InterruptWaitStateAndPowerControlRegisters _interruptWaitStateAndPowerControlRegisters = new();
    private readonly static TestDebugger _testDebugger = new();
    private readonly static SerialController _serialController = new(_testDebugger);

    [Theory]
    [InlineData(0, 0, 0, false)]
    [InlineData(1, 0, 1, false)]
    [InlineData(1, 1, 2, false)]
    [InlineData(0x8000_0000, 1, 0, true)]
    [InlineData(0x8000_0000, 2, 0, false)]
    public void TestLSL(uint rs, int offset, uint expected, bool expectedCarry)
    {
        var bus = new MemoryBus(_bios, _testGamepad, _testGamePak, _testPpu, _testDmaController, _testTimerController, _interruptWaitStateAndPowerControlRegisters, _serialController, _testDebugger);
        var cpu = new Core(bus, 0, _testDebugger);
        cpu.R[1] = rs;

        Thumb.LSL_Imm(cpu, (ushort)(((offset & 0b1_1111) << 6) | 0b00_1000));

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
        var bus = new MemoryBus(_bios, _testGamepad, _testGamePak, _testPpu, _testDmaController, _testTimerController, _interruptWaitStateAndPowerControlRegisters, _serialController, _testDebugger);
        var cpu = new Core(bus, 0, _testDebugger);
        cpu.R[1] = rs;

        Thumb.LSR_Imm(cpu, (ushort)(((offset & 0b1_1111) << 6) | 0b1000_0000_1000));

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
        var bus = new MemoryBus(_bios, _testGamepad, _testGamePak, _testPpu, _testDmaController, _testTimerController, _interruptWaitStateAndPowerControlRegisters, _serialController, _testDebugger);
        var cpu = new Core(bus, 0, _testDebugger);
        cpu.R[1] = rs;

        Thumb.ASR_Imm(cpu, (ushort)(((offset & 0b1_1111) << 6) | 0b1_0000_0000_1000));

        Assert.Equal(expected, cpu.R[0]);
        Assert.Equal(expectedCarry, cpu.Cpsr.CarryFlag);
    }
}
