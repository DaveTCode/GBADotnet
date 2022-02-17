﻿using GameboyAdvanced.Core.Cpu.Interrupts;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Dma;
using GameboyAdvanced.Core.Input;
using GameboyAdvanced.Core.Rom;
using GameboyAdvanced.Core.Timer;
using System;
using Xunit;

namespace GameboyAdvanced.Core.Tests;

public class SwpTests
{
    private readonly static byte[] _bios = new byte[0x4000];
    private readonly static Ppu.Ppu _testPpu = new();
    private readonly static GamePak _testGamePak = new(new byte[0xFF_FFFF]);
    private readonly static Gamepad _testGamepad = new();
    private readonly static DmaController _testDmaController = new();
    private readonly static TimerController _testTimerController = new();
    private readonly static InterruptWaitStateAndPowerControlRegisters _interruptWaitStateAndPowerControlRegisters = new();
    private readonly static TestDebugger _testDebugger = new();

    [Fact]
    public void TestSwp()
    {
        Array.Clear(_bios);
        var instruction = 0b1110_0001_0000_0000_0001_0000_1001_0010; // SWP R1, R2, [R0]
        Utils.WriteWord(_bios, 0xFFFF, 0x0, instruction);
        var bus = new MemoryBus(_bios, _testGamepad, _testGamePak, _testPpu, _testDmaController, _testTimerController, _interruptWaitStateAndPowerControlRegisters, _testDebugger);
        var cpu = new Core(bus, 0, _testDebugger);
        cpu.R[0] = 0x0300_1000u; // Rn is the swap address in memory
        cpu.R[1] = 0xBEEF_FEEDu; // Set up value to swap into memory
        cpu.R[2] = 0xFEED_BEEFu; // This value should be overwritten
        _ = cpu.Bus.WriteWord(0x0300_1000, 0xABCD_EFFFu);

        cpu.Clock(); // Fill decode stage of pipeline, not really part of this instruction
        cpu.Clock(); // Fill execute stage of pipeline and perform address translation
        Assert.Equal(0x0300_1000u, cpu.R[0]);
        Assert.Equal(0xBEEF_FEEDu, cpu.R[1]);
        Assert.Equal(0xFEED_BEEFu, cpu.R[2]);
        Assert.Equal(0x0300_1000u, cpu.A); // First cycle should have the ALU calculated address on the address bus
        cpu.Clock(); // 2nd clock of instruction, should now see value on data bus but register shouldn't be loaded yet
        Assert.Equal(0x0300_1000u, cpu.R[0]);
        Assert.Equal(0xBEEF_FEEDu, cpu.R[1]);
        Assert.Equal(0xFEED_BEEFu, cpu.R[2]);
        Assert.Equal(0xABCD_EFFFu, cpu.Bus.ReadWord(0x0300_1000).Item1);
        cpu.Clock(); // 3rd clock, memory has been written but registers all still show initial values
        Assert.Equal(0x0300_1000u, cpu.R[0]);
        Assert.Equal(0xBEEF_FEEDu, cpu.R[1]);
        Assert.Equal(0xFEED_BEEFu, cpu.R[2]);
        Assert.Equal(0xFEED_BEEFu, cpu.Bus.ReadWord(0x0300_1000).Item1);
        cpu.Clock(); // 4th clock of instruction, register should now contain memory value
        Assert.Equal(0x0300_1000u, cpu.R[0]);
        Assert.Equal(0xABCD_EFFFu, cpu.R[1]);
        Assert.Equal(0xFEED_BEEFu, cpu.R[2]);
        Assert.Equal(0xFEED_BEEFu, cpu.Bus.ReadWord(0x0300_1000).Item1);

        Assert.Equal(1u + 4, cpu.Cycles); // 1 for pipeline + 1S + 2N + 1I cycles to execute
    }

    [Fact]
    public void TestSwpb()
    {
        Array.Clear(_bios);
        var instruction = 0b1110_0001_0100_0000_0001_0000_1001_0010; // SWPB R1, R2, [R0]
        Utils.WriteWord(_bios, 0xFFFF, 0x0, instruction);
        var bus = new MemoryBus(_bios, _testGamepad, _testGamePak, _testPpu, _testDmaController, _testTimerController, _interruptWaitStateAndPowerControlRegisters, _testDebugger);
        var cpu = new Core(bus, 0, _testDebugger);
        cpu.R[0] = 0x0300_1000u; // Rn is the swap address in memory
        cpu.R[1] = 0xBEEF_FEEDu; // Set up value to swap into memory
        cpu.R[2] = 0xFEED_BEEFu; // This value should be overwritten
        _ = cpu.Bus.WriteWord(0x0300_1000, 0xABCD_EFFEu);

        cpu.Clock(); // Fill decode stage of pipeline, not really part of this instruction
        cpu.Clock(); // Fill execute stage of pipeline and perform address translation
        Assert.Equal(0x0300_1000u, cpu.R[0]);
        Assert.Equal(0xBEEF_FEEDu, cpu.R[1]);
        Assert.Equal(0xFEED_BEEFu, cpu.R[2]);
        Assert.Equal(0x0300_1000u, cpu.A); // First cycle should have the ALU calculated address on the address bus
        cpu.Clock(); // 2nd clock of instruction, should now see value on data bus but register shouldn't be loaded yet
        Assert.Equal(0x0300_1000u, cpu.R[0]);
        Assert.Equal(0xBEEF_FEEDu, cpu.R[1]);
        Assert.Equal(0xFEED_BEEFu, cpu.R[2]);
        Assert.Equal(0xABCD_EFFE, cpu.Bus.ReadWord(0x0300_1000).Item1);
        cpu.Clock(); // 3rd clock, memory has been written but registers all still show initial values
        Assert.Equal(0x0300_1000u, cpu.R[0]);
        Assert.Equal(0xBEEF_FEEDu, cpu.R[1]);
        Assert.Equal(0xFEED_BEEFu, cpu.R[2]);
        Assert.Equal(0xABCD_EFEFu, cpu.Bus.ReadWord(0x0300_1000).Item1);
        cpu.Clock(); // 4th clock of instruction, register should now contain memory value
        Assert.Equal(0x0300_1000u, cpu.R[0]);
        Assert.Equal(0x0000_00FEu, cpu.R[1]);
        Assert.Equal(0xFEED_BEEFu, cpu.R[2]);
        Assert.Equal(0xABCD_EFEFu, cpu.Bus.ReadWord(0x0300_1000).Item1);

        Assert.Equal(1u + 4, cpu.Cycles); // 1 for pipeline + 1S + 2N + 1I cycles to execute
    }
}