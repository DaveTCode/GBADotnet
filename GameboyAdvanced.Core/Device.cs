namespace GameboyAdvanced.Core;

/// <summary>
/// The device class is the externally facing interface into the emulator
/// core.
/// </summary>
public class Device
{
    private const int CPU_CYCLES_PER_FRAME = 280896;

    private readonly MemoryBus _bus;
    private readonly Arm7Tdmi _cpu;
    private readonly Ppu _ppu;

    public Device(byte[] bios)
    {
        _ppu = new Ppu();
        _bus = new MemoryBus(bios, _ppu);
        _cpu = new Arm7Tdmi(_bus);
    }

    public int RunFrame(int overflowCycles)
    {
        var cycles = overflowCycles;
        while (cycles < CPU_CYCLES_PER_FRAME)
        {
            var instructionCycles = _cpu.SingleInstruction();
            _ppu.Step(instructionCycles);
            cycles += instructionCycles;
        }
        
        return CPU_CYCLES_PER_FRAME - cycles;
    }
}
