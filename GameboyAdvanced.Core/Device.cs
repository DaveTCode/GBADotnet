using GameboyAdvanced.Core.Cpu.Interrupts;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Dma;
using GameboyAdvanced.Core.Input;
using GameboyAdvanced.Core.Rom;
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
    private readonly DmaController _dma;
    private readonly GamePak _gamepak;
    private readonly Gamepad _gamepad;
    private readonly Ppu.Ppu _ppu;
    private readonly TimerController _timerController;
    private readonly InterruptWaitStateAndPowerControlRegisters _interruptController;

    private delegate*<Device, void> _nextPpuAction = null;
    private delegate*<Device, void> _nextApuAction = null;
    private delegate*<Device, void> _nextTimerAction = null;
    private delegate*<Device, void> _nextDmaAction = null;

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
    /// <param name="startVector">
    /// The initial value of R[15], i.e. the first instruction that will be fetched.
    /// 
    /// Typically either 0x0000_0000 (RESET vector) in BIOS or 0x0800_0000 
    /// (default start of ROM).
    /// </param>
    public Device(byte[] bios, GamePak rom, BaseDebugger debugger, uint startVector = 0x0000_0000)
    {
        _gamepak = rom;
        _interruptController = new InterruptWaitStateAndPowerControlRegisters();
        _gamepad = new Gamepad();
        _timerController = new TimerController();
        _ppu = new Ppu.Ppu();
        _dma = new DmaController();
        Bus = new MemoryBus(bios, _gamepad, _gamepak, _ppu, _dma, _timerController, _interruptController, debugger);
        _cpu = new Core(Bus, startVector, debugger);
        Debugger = debugger;
    }

    public void RunCycle(bool skipBreakpoints=false)
    {
        #if DEBUG
        if (!skipBreakpoints && Debugger.CheckBreakpoints(_cpu))
        {
            throw new BreakpointException();
        }
        #endif
        _cpu.Clock();
        _ppu.Step(1);
    }

    public void Reset(uint startVector = 0x0000_0000)
    {
        _cpu.Reset(startVector);
        Bus.Reset();
        _ppu.Reset();
        _dma.Reset();
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

    public uint InspectWord(uint address) => Bus.ReadWord(address).Item1;

    public ushort InspectHalfWord(uint address) => Bus.ReadHalfWord(address).Item1;

    public byte InspectByte(uint address) => Bus.ReadByte(address).Item1;

    public string LoadedRomName() => _gamepak.GameTitle;

    public override string ToString() => _cpu.ToString();
}
