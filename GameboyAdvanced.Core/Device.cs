using GameboyAdvanced.Core.Bus;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Dma;
using GameboyAdvanced.Core.Input;
using GameboyAdvanced.Core.Interrupts;
using GameboyAdvanced.Core.Rom;
using GameboyAdvanced.Core.Serial;
using GameboyAdvanced.Core.Timer;

namespace GameboyAdvanced.Core;

/// <summary>
/// The device class is the externally facing interface into the emulator
/// core.
/// </summary>
public unsafe class Device
{
    public const int WIDTH = 240;
    public const int HEIGHT = 160;
    private const int CPU_CYCLES_PER_FRAME = 280896;

    public readonly BaseDebugger Debugger;

    public readonly MemoryBus Bus;
    public readonly Core Cpu;
    public readonly DmaDataUnit DmaData;
    public readonly DmaController DmaCtrl;
    public readonly GamePak Gamepak;
    public readonly Gamepad Gamepad;
    public readonly Ppu.Ppu Ppu;
    public readonly Apu.Apu Apu;
    public readonly TimerController TimerController;
    public readonly InterruptRegisters InterruptRegisters;
    public readonly InterruptInterconnect InterruptInterconnect;
    public readonly SerialController SerialController;

    /// <summary>
    /// Constructs a device which can be used to exectue a rom by running 
    /// functions like "RunFrame"/"RunCycle".
    /// </summary>
    /// 
    /// <param name="bios">
    /// A byte array containing a valid GBA bios (or equivalent open 
    /// source implementation).
    /// </param>
    /// 
    /// <param name="rom">
    /// A byte array containing a valid GBA rom as parsed with 
    /// `new GamePak(byte[])`.
    /// </param>
    /// 
    /// <param name="debugger">
    /// A debug hook which can be set to <see cref="TestDebugger"/> if no debugging is required.
    /// 
    /// Only used when compiled with DEBUG set.
    /// </param>
    /// 
    /// <param name="skipBios">
    /// Determines whether to start operation at 0x0 (start of bios) or at 
    /// 0x0800_0000 with registers/banks set up as if the bios had run.
    /// </param>
    public Device(byte[] bios, GamePak rom, BaseDebugger debugger, bool skipBios)
    {
        Gamepak = rom;
        DmaData = new DmaDataUnit();
        InterruptRegisters = new InterruptRegisters();
        InterruptInterconnect = new InterruptInterconnect(debugger, InterruptRegisters);
        Gamepad = new Gamepad(debugger, InterruptInterconnect);
        TimerController = new TimerController(debugger, InterruptInterconnect);
        Ppu = new Ppu.Ppu(debugger, InterruptInterconnect, DmaData);
        Apu = new Apu.Apu(debugger);
        SerialController = new SerialController(debugger, InterruptInterconnect);
        Bus = new MemoryBus(bios, Gamepad, Gamepak, Ppu, Apu, DmaData, TimerController, InterruptRegisters, SerialController, debugger, skipBios);
        Cpu = new Core(Bus, skipBios, debugger, InterruptRegisters);
        DmaCtrl = new DmaController(Bus, debugger, DmaData, InterruptInterconnect, Ppu, Cpu);
        Debugger = debugger;
    }

    public void RunCycle(bool skipBreakpoints=false)
    {
        Cpu.Cycles++;
#if DEBUG
        if (!skipBreakpoints && Debugger.CheckBreakpoints(Cpu))
        {
            throw new BreakpointException();
        }
#endif
        TimerController.Step();

        if (Bus.HaltMode == HaltMode.None)
        {
            if (Bus.WaitStates > 0)
            {
                Bus.WaitStates--;
            }
            else
            {
                if (!DmaCtrl.Step())
                {
                    // Only step the CPU unit if the DMA is inactive
                    Cpu.Clock();
                }
            }
        }
        else if (InterruptRegisters.ShouldBreakHalt)
        {
            Bus.HaltMode = HaltMode.None;
            Bus.WaitStates = 1;
        }

        Ppu.Step();
    }

    public void Reset(bool skipBios)
    {
        Cpu.Reset(skipBios);
        Bus.Reset(skipBios);
        Ppu.Reset();
        DmaCtrl.Reset();
        DmaData.Reset();
        TimerController.Reset();
        Gamepad.Reset();
    }

    public void RunFrame()
    {
        for (var ii = 0; ii < CPU_CYCLES_PER_FRAME; ii++)
        {
            RunCycle();
        }
    }

    public byte[] GetFrame() => Ppu.GetFrame();

    public void PressKey(Key key) => Gamepad.PressKey(key);

    public void ReleaseKey(Key key) => Gamepad.ReleaseKey(key);

    public uint InspectWord(uint address)
    {
        return Bus.ReadWord(address, 0, 0, 0, 0);
    }

    public ushort InspectHalfWord(uint address)
    {
        return Bus.ReadHalfWord(address, 0, 0, 0, 0);
    }

    public byte InspectByte(uint address)
    {
        return Bus.ReadByte(address, 0, 0, 0, 0);
    }

    public string LoadedRomName() => Gamepak.GameTitle;

    public override string ToString() => Cpu.ToString();
}
