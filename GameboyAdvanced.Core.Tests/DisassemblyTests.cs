using GameboyAdvanced.Core.Bus;
using GameboyAdvanced.Core.Cpu.Disassembler;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Dma;
using GameboyAdvanced.Core.Input;
using GameboyAdvanced.Core.Interrupts;
using GameboyAdvanced.Core.Rom;
using GameboyAdvanced.Core.Serial;
using GameboyAdvanced.Core.Timer;
using Xunit;

namespace GameboyAdvanced.Core.Tests;

public class DisassemblyTests
{
    private readonly static byte[] _bios = new byte[0x4000];
    private readonly static GamePak _testGamePak = new(new byte[0xFF_FFFF]);
    private readonly static DmaDataUnit _testDmaDataUnit = new();
    private readonly static InterruptRegisters _interruptRegisters = new();
    private readonly static TestDebugger _testDebugger = new();
    private readonly static InterruptInterconnect _interruptInterconnect = new(_testDebugger, _interruptRegisters);
    private readonly static Gamepad _testGamepad = new(_testDebugger, _interruptInterconnect);
    private readonly static Apu.Apu _testApu = new(_testDebugger);
    private readonly static Ppu.Ppu _testPpu = new(_testDebugger, _interruptInterconnect, _testDmaDataUnit);
    private readonly static TimerController _testTimerController = new(_testDebugger, _interruptInterconnect);
    private readonly static SerialController _serialController = new(_testDebugger, _interruptInterconnect);

    [Theory]
    [InlineData(0xE3110001, "TST r1, #1")]
    [InlineData(0xE28fe004, "ADD LR, PC, #4")]
    [InlineData(0x03A0E004, "MOVEQ LR, #4")]
    [InlineData(0x010FC000, "MRSEQ R12, CPSR")]
    [InlineData(0x0AFFFFFC, "BEQ #08001D58")]
    [InlineData(0xE12FFF1E, "BX LR=080002CC")]
    public void TestArmDisassembly(uint instruction, string expected)
    {
        var bus = new MemoryBus(_bios, _testGamepad, _testGamePak, _testPpu, _testApu, _testDmaDataUnit, _testTimerController, _interruptRegisters, _serialController, _testDebugger, false);
        var cpu = new Core(bus, false, _testDebugger, _interruptRegisters);
        // r0:04000000   r1:00110000   r2:00000000   r3:030045FB
        // r4:03004850   r5:03004840   r6:03004870   r7:030045FB
        // r8:0000000A   r9:00000000  r10:00000000  r11:03004880
        // r12:00000212  r13:03007EF8  r14:080002CC  r15:08001D68
        cpu.R[0] = 0x04000000; cpu.R[1] = 0x00110000; cpu.R[2] = 0x00000000; cpu.R[3] = 0x030045FB;
        cpu.R[4] = 0x03004850; cpu.R[5] = 0x03004840; cpu.R[6] = 0x03004870; cpu.R[7] = 0x030045FB;
        cpu.R[8] = 0x0000000A; cpu.R[9] = 0x00000000; cpu.R[10] = 0x00000000; cpu.R[11] = 0x03004880;
        cpu.R[12] = 0x00000212; cpu.R[13] = 0x03007EF8; cpu.R[14] = 0x080002CC; cpu.R[15] = 0x08001D68;

        Assert.Equal(expected, ArmDisassembler.Disassemble(cpu, instruction), ignoreCase: true, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
    }
}
