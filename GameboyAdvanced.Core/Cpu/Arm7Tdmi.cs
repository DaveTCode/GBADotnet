namespace GameboyAdvanced.Core;

internal class Arm7Tdmi
{
    internal readonly MemoryBus Bus;
    internal CPSR Cpsr;
    internal readonly uint[] R = new uint[16];

    internal Arm7Tdmi(MemoryBus bus)
    {
        Bus = bus;
        Reset();
    }

    internal void Reset()
    {
        Array.Clear(R, 0, R.Length);
    }

    internal int SingleInstruction()
    {
        while (true)
        {
            if (Cpsr.ThumbMode)
            {
                var (instruction, cycles) = Bus.ReadHalfWord(R[15]);
            }
            else
            {
                var (instruction, cycles) = Bus.ReadWord(R[15]);
                var cond = instruction >> 28;
                
            }
        }
    }
}
