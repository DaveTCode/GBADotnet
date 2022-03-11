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

    internal readonly MemoryBus Bus;
    private readonly Core _cpu;
    private readonly DmaDataUnit _dmaData;
    private readonly DmaController _dmaCtrl;
    private readonly GamePak _gamepak;
    private readonly Gamepad _gamepad;
    private readonly Ppu.Ppu _ppu;
    private readonly Apu.Apu _apu;
    private readonly TimerController _timerController;
    private readonly InterruptRegisters _interruptRegisters;
    private readonly InterruptInterconnect _interruptInterconnect;
    private readonly SerialController _serialController;

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
        _gamepak = rom;
        _interruptRegisters = new InterruptRegisters();
        _interruptInterconnect = new InterruptInterconnect(debugger, _interruptRegisters);
        _gamepad = new Gamepad(debugger, _interruptInterconnect);
        _timerController = new TimerController(debugger, _interruptInterconnect);
        _ppu = new Ppu.Ppu(debugger, _interruptInterconnect);
        _apu = new Apu.Apu(debugger);
        _dmaData = new DmaDataUnit();
        _serialController = new SerialController(debugger, _interruptInterconnect);
        Bus = new MemoryBus(bios, _gamepad, _gamepak, _ppu, _apu, _dmaData, _timerController, _interruptRegisters, _serialController, debugger, skipBios);
        _cpu = new Core(Bus, skipBios, debugger, _interruptRegisters);
        _dmaCtrl = new DmaController(Bus, debugger, _dmaData, _interruptInterconnect, _ppu, _cpu);
        Debugger = debugger;
    }

    public void RunCycle(bool skipBreakpoints=false)
    {
        _cpu.Cycles++;
#if DEBUG
        if (!skipBreakpoints && Debugger.CheckBreakpoints(_cpu))
        {
            throw new BreakpointException();
        }
#endif
        _timerController.Step();
        if (!_dmaCtrl.Step())
        {
            // Only step the CPU unit if the DMA is inactive
            _cpu.Clock();
        }
        _ppu.Step();
    }

    public void Reset(bool skipBios)
    {
        _cpu.Reset(skipBios);
        Bus.Reset(skipBios);
        _ppu.Reset();
        _dmaCtrl.Reset();
        _dmaData.Reset();
        _timerController.Reset();
        _gamepad.Reset();
    }

    public void RunFrame()
    {
        for (var ii = 0; ii < CPU_CYCLES_PER_FRAME; ii++)
        {
            RunCycle();
        }
    }

    public byte[] GetFrame() => _ppu.GetFrame();

    public void PressKey(Key key) => _gamepad.PressKey(key);

    public void ReleaseKey(Key key) => _gamepad.ReleaseKey(key);

    public uint InspectWord(uint address)
    {
        int waitStates = 0;
        return Bus.ReadWord(address, 0, 0, 0, 0, ref waitStates);
    }

    public ushort InspectHalfWord(uint address)
    {
        int waitStates = 0;
        return Bus.ReadHalfWord(address, 0, 0, 0, 0, ref waitStates);
    }

    public byte InspectByte(uint address)
    {
        int waitStates = 0;
        return Bus.ReadByte(address, 0, 0, 0, 0, ref waitStates);
    }

    public string LoadedRomName() => _gamepak.GameTitle;

    public override string ToString() => _cpu.ToString();
}
