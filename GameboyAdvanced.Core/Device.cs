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

    /// <summary>
    /// Track the total number of cycles executed, purely for debugging
    /// </summary>
    private long _cycles;

    internal readonly MemoryBus Bus;
    private readonly Core _cpu;
    private readonly DmaController _dma;
    private readonly GamePak _rom;
    private readonly Gamepad _gamepad;
    private readonly Ppu.Ppu _ppu;
    private readonly TimerController _timerController;

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
    /// <param name="startVector">
    /// The initial value of R[15], i.e. the first instruction that will be fetched.
    /// 
    /// Typically either 0x0000_0000 (RESET vector) in BIOS or 0x0800_0000 
    /// (default start of ROM).
    /// </param>
    public Device(byte[] bios, GamePak rom, uint startVector = 0x0000_0000)
    {
        _rom = rom;
        _gamepad = new Gamepad();
        _timerController = new TimerController();
        _ppu = new Ppu.Ppu();
        _dma = new DmaController();
        Bus = new MemoryBus(bios, _gamepad, _rom, _ppu, _dma, _timerController);
        _cpu = new Core(Bus, startVector);
    }

    public void RunCycle()
    {
        _cpu.Clock();
        _ppu.Step(1);
    }

    public int RunFrame(int overflowCycles)
    {
        var cycles = overflowCycles;
        while (cycles < CPU_CYCLES_PER_FRAME)
        {
            RunCycle();
            cycles += 1;
        }
        
        return CPU_CYCLES_PER_FRAME - cycles;
    }

    public byte[] GetFrame() => _ppu.GetFrame();

    public void PressKey(Key key) => _gamepad.PressKey(key);

    public void ReleaseKey(Key key) => _gamepad.ReleaseKey(key);
}
