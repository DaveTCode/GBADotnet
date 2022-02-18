using GameboyAdvanced.Core.Cpu.Interrupts;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Dma;
using GameboyAdvanced.Core.Input;
using GameboyAdvanced.Core.Rom;
using GameboyAdvanced.Core.Serial;
using GameboyAdvanced.Core.Timer;
using System;
using Xunit;

namespace GameboyAdvanced.Core.Tests;

public class LdrStrTests
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
    [InlineData(true, 0xED)]
    [InlineData(false, 0xBEEF_FEEDu)]
    public void TestThumbLdr(bool byteWidth, uint loadVal)
    {
        Array.Clear(_bios);
        var instruction = 0b0101_1000_0100_0010; // LDR R2, [R0, R1]
        if (byteWidth) instruction |= 0b0000_0100_0000_0000;
        Utils.WriteHalfWord(_bios, 0xFFFF, 0x0, (ushort)instruction);
        var bus = new MemoryBus(_bios, _testGamepad, _testGamePak, _testPpu, _testDmaController, _testTimerController, _interruptWaitStateAndPowerControlRegisters, _serialController, _testDebugger);
        var cpu = new Core(bus, 0, _testDebugger);
        cpu.Cpsr.ThumbMode = true;
        cpu.R[0] = 0x0300_1000u; // Set up where we're writing to
        cpu.R[1] = 0x0000_0004u; // Set up offset (so actual write will be to 0x0300_0001)
        cpu.R[2] = 0x1234_5678u;
        _ = cpu.Bus.WriteWord(0x0300_1004, 0xBEEF_FEEDu);

        cpu.Clock(); // Fill decode stage of pipeline, not really part of this instruction
        cpu.Clock(); // Fill execute stage of pipeline and perform address translation
        Assert.Equal(0x1234_5678u, cpu.R[2]);
        Assert.Equal(0x0300_1004u, cpu.A); // First cycle should have the ALU calculated address on the address bus
        cpu.Clock(); // 2nd clock of instruction, should now see value on data bus but register shouldn't be loaded yet
        Assert.Equal(0x1234_5678u, cpu.R[2]);
        Assert.Equal(loadVal, cpu.D); // TODO - Not actually sure this is right, I expect D will have more than just the byte casted value
        Assert.True(cpu.nMREQ); // On the 2nd cycle nMREQ is driven high and no memory is read on the 3rd cycle

        cpu.Clock(); // 3rd clock of instruction, register should now be reloaded and system should be ready for opcode fetch
        Assert.Equal(loadVal, cpu.R[2]);
        Assert.False(cpu.nRW);
        Assert.False(cpu.nOPC);
        Assert.False(cpu.nMREQ);
        Assert.False(cpu.SEQ);
        
        Assert.Equal(1u + 3, cpu.Cycles); // 1 for pipeline + 2N + 1S cycles
    }
}
