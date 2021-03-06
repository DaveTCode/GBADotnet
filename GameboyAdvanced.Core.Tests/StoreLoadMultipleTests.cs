using GameboyAdvanced.Core.Bus;
using GameboyAdvanced.Core.Interrupts;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Dma;
using GameboyAdvanced.Core.Input;
using GameboyAdvanced.Core.Rom;
using GameboyAdvanced.Core.Serial;
using GameboyAdvanced.Core.Timer;
using System;
using Xunit;

namespace GameboyAdvanced.Core.Tests;

public class StoreLoadMultipleTests
{
    private readonly static byte[] _bios = new byte[0x4000];
    private readonly static GamePak _testGamePak = new(new byte[0xFF_FFFF]);
    private readonly static Device _testDevice = new(_bios, _testGamePak, new TestDebugger(), true);
    private readonly static DmaDataUnit _testDmaDataUnit = new();
    private readonly static InterruptRegisters _interruptRegisters = new(_testDevice);
    private readonly static TestDebugger _testDebugger = new();
    private readonly static InterruptInterconnect _interruptInterconnect = new(_testDebugger, _interruptRegisters);
    private readonly static Gamepad _testGamepad = new(_testDebugger, _interruptInterconnect);
    private readonly static Ppu.Ppu _testPpu = new(_testDebugger);
    private readonly static Apu.Apu _testApu = new(_testDevice, _testDebugger);
    private readonly static TimerController _testTimerController = new(_testDevice, _testDebugger);
    private readonly static SerialController _serialController = new(_testDebugger, _interruptInterconnect);

    [Fact]
    public void TestThumbStmia()
    {
        Array.Clear(_bios);
        var instruction = 0b1100_0000_1111_1110; // STMIA R0!, [r1-r7]
        _bios[0] = (byte)(instruction & 0xFF);
        _bios[1] = (byte)((instruction >> 8) & 0xFF);
        var bus = new MemoryBus(_bios, _testGamepad, _testGamePak, _testPpu, _testApu, _testDmaDataUnit, _testTimerController, _interruptRegisters, _serialController, _testDebugger, false);
        var cpu = new Core(bus, false, _testDebugger, _interruptRegisters);
        cpu.Cpsr.ThumbMode = true;
        cpu.R[0] = 0x0300_1000u; // Set up where we're writing to
        for (var r = 1u; r < 8; r++)
        {
            cpu.R[r] = r;
        }

        cpu.Clock(); cpu.Clock(); // Fill decode stage of pipeline;
        cpu.Clock(); // Fill execute stage of pipeline and perform address translation
        Assert.Equal(0x0300_1000u, cpu.A);
        Assert.Equal(1u, cpu.D); // First cycle should have set r[1] on the data bus

        for (var r = 1u; r < 7; r++)
        {
            cpu.Clock();
            Assert.Equal(0x0300_1000u + (4u * r), cpu.A);
            Assert.Equal(r + 1, cpu.D);
        }

        cpu.Clock();
        Assert.False(cpu.nRW);
        Assert.False(cpu.nOPC);
        Assert.False(cpu.nMREQ);
        Assert.Equal(0, cpu.SEQ);
        Assert.Equal(0x0300_101Cu, cpu.R[0]);

        for (var r = 1u; r < 8; r++)
        {
            Assert.Equal(r, cpu.Bus.ReadWord(0x0300_1000u + (4u * (r - 1)), 0, 0, 0, 0, false));
        }
    }

    [Fact]
    public void TestArmStmia()
    {
        Array.Clear(_bios);
        //                  cond op_p UsWl Rn__ reglist
        var instruction = 0b1110_1000_1010_0000_0000_0000_1111_1110; // STMIA R0!, [r1-r7]
        _bios[0] = (byte)(instruction & 0xFF);
        _bios[1] = (byte)((instruction >> 8) & 0xFF);
        _bios[2] = (byte)((instruction >> 16) & 0xFF);
        _bios[3] = (byte)((instruction >> 24) & 0xFF);
        var bus = new MemoryBus(_bios, _testGamepad, _testGamePak, _testPpu, _testApu, _testDmaDataUnit, _testTimerController, _interruptRegisters, _serialController, _testDebugger, false);
        var cpu = new Core(bus, false, _testDebugger, _interruptRegisters);
        cpu.R[0] = 0x0300_1000u; // Set up where we're writing to
        for (var r = 1u; r < 8; r++)
        {
            cpu.R[r] = r;
        }

        cpu.Clock(); cpu.Clock(); // Fill decode stage of pipeline;
        cpu.Clock(); // Fill execute stage of pipeline and perform address translation
        Assert.Equal(0x0300_1000u, cpu.A);
        Assert.Equal(1u, cpu.D); // First cycle should have set r[1] on the data bus

        for (var r = 1u; r < 7; r++)
        {
            cpu.Clock();
            Assert.Equal(0x0300_1000u + (4u * r), cpu.A);
            Assert.Equal(r + 1, cpu.D);
        }

        cpu.Clock();
        Assert.False(cpu.nRW);
        Assert.False(cpu.nOPC);
        Assert.False(cpu.nMREQ);
        Assert.Equal(0, cpu.SEQ);
        Assert.Equal(0x0300_101Cu, cpu.R[0]);

        for (var r = 1u; r < 8; r++)
        {
            Assert.Equal(r, cpu.Bus.ReadWord(0x0300_1000u + (4u * (r - 1)), 0, 0, 0, 0, false));
        }
    }

    [Fact]
    public void TestThumbLdmia()
    {
        var instruction = 0b1100_1000_1111_1110; // LDMIA R0!, [r1-r7]
        _bios[0] = (byte)(instruction & 0xFF);
        _bios[1] = (byte)((instruction >> 8) & 0xFF);
        var bus = new MemoryBus(_bios, _testGamepad, _testGamePak, _testPpu, _testApu, _testDmaDataUnit, _testTimerController, _interruptRegisters, _serialController, _testDebugger, false);
        var cpu = new Core(bus, false, _testDebugger, _interruptRegisters);
        cpu.Cpsr.ThumbMode = true;
        cpu.R[0] = 0x0300_1000u; // Set up where we're writing to
        for (var r = 0u; r < 7; r++)
        {
            cpu.Bus.WriteWord(0x0300_1000u + (r * 4), r + 1, 1, 0);
        }

        cpu.Clock(); cpu.Clock(); // Fill decode stage of pipeline;
        cpu.Clock(); // Fill execute stage of pipeline and perform address translation
        Assert.Equal(0x0300_1000u, cpu.A);
        cpu.Clock();
        Assert.Equal(0x0300_1004u, cpu.A);
        Assert.Equal(0u, cpu.R[1]); // R1 shouldn't be loaded until the next cycle
        Assert.Equal(1u, cpu.D); // But the data bus should see what will go into R1

        for (var r = 0u; r < 5; r++)
        {
            cpu.Clock();
            Assert.Equal(0x0300_1008u + (4u * r), cpu.A);
            Assert.Equal(r + 2, cpu.D);
            Assert.Equal(r + 1, cpu.R[r + 1]);
        }

        cpu.Clock();
        Assert.Equal(6u, cpu.R[6]);
        Assert.Equal(0u, cpu.R[7]);
        cpu.Clock();
        Assert.Equal(0x0300_101Cu, cpu.R[0]); // Final writeback value
        Assert.Equal(7u, cpu.D); // D should still show 7 because no mem req made last cycle
        Assert.Equal(7u, cpu.R[7]);
        Assert.False(cpu.nRW);
        Assert.False(cpu.nOPC);
        Assert.False(cpu.nMREQ);
        Assert.Equal(0, cpu.SEQ);
    }
}
