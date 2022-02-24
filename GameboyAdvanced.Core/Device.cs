using GameboyAdvanced.Core.Bus;
using GameboyAdvanced.Core.Cpu.Interrupts;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Dma;
using GameboyAdvanced.Core.Input;
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
    private readonly TimerController _timerController;
    private readonly InterruptWaitStateAndPowerControlRegisters _interruptController;
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
        _dmaData = new DmaDataUnit();
        _serialController = new SerialController(debugger);
        Bus = new MemoryBus(bios, _gamepad, _gamepak, _ppu, _dmaData, _timerController, _interruptController, _serialController, debugger);
        _dmaCtrl = new DmaController(Bus, debugger, _dmaData);
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
        if (!_dmaCtrl.Step())
        {
            // Only step the CPU unit if the DMA is inactive
            _cpu.Clock();
        }
        _ppu.Step();
    }

    public void Reset(uint startVector = 0x0000_0000)
    {
        _cpu.Reset(startVector);
        Bus.Reset();
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

    public uint InspectWord(uint address) => Bus.ReadWord(address).Item1;

    public ushort InspectHalfWord(uint address) => Bus.ReadHalfWord(address).Item1;

    public byte InspectByte(uint address) => Bus.ReadByte(address).Item1;

    public string LoadedRomName() => _gamepak.GameTitle;

    public override string ToString() => _cpu.ToString();
}
