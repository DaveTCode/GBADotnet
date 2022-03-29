using GameboyAdvanced.Core;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Input;
using GameboyAdvanced.Core.Rom;
using GameboyAdvanced.Web.Signalr;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace GameboyAdvanced.Web.Emulation;

public enum BackgroundEmulatorState
{
    WaitingForRom,
    Paused,
    Running,
}

public class BackgroundEmulatorThread
{
    private readonly ILogger<BackgroundEmulatorThread> _logger;
    private readonly IHubContext<EmulatorHub, IEmulatorClient> _emulatorHubContext;
    private readonly SemaphoreSlim _romSemaphore = new(1);
    private GamePak? _gamePak = null;
    private readonly byte[] _bios;
    private Device? _device;

    private readonly SemaphoreSlim _keyStateSemaphore = new(1);
    private readonly Dictionary<Key, bool> _keyState = new()
    {
        { Key.A, false },
        { Key.B, false },
        { Key.Select, false },
        { Key.Start, false },
        { Key.Up, false },
        { Key.Down, false },
        { Key.Left, false },
        { Key.Right, false },
        { Key.L, false },
        { Key.R, false },
    };

    private volatile BackgroundEmulatorState _state;

    public BackgroundEmulatorThread(
        ILogger<BackgroundEmulatorThread> logger,
        IHubContext<EmulatorHub, IEmulatorClient> emulatorHubContext,
        byte[] bios)
    {
        _logger = logger;
        _emulatorHubContext = emulatorHubContext;
        _bios = bios;
    }

    public async Task RunRomAsync(byte[] rom)
    {
        var acquiredSemaphore = await _romSemaphore.WaitAsync(5000);
        if (!acquiredSemaphore)
        {
            _logger.LogError("Can't acquire rom semaphore to load rom");
        }

        _gamePak = new GamePak(rom);

        _ = _romSemaphore.Release();
        _state = BackgroundEmulatorState.WaitingForRom;
    }

    private async Task<Device?> WaitForRomLoadAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var acquiredSemaphore = await _romSemaphore.WaitAsync(5000, cancellationToken);
            if (!acquiredSemaphore)
            {
                _logger.LogError("Can't acquire rom semaphore to check for loaded rom");
            }

            if (_gamePak != null) 
            {
                var device = new Device(_bios, _gamePak, new TestDebugger(), true);
                _state = BackgroundEmulatorState.Running;
                _ = _romSemaphore.Release();
                return device;
            }

            _ = _romSemaphore.Release();
            await Task.Delay(5000, cancellationToken);
        }

        return null;
    }

    public async Task KeyUp(Key key)
    {
        await _keyStateSemaphore.WaitAsync();
        _keyState[key] = false;
        _ = _keyStateSemaphore.Release();
    }

    public async Task KeyDown(Key key)
    {
        await _keyStateSemaphore.WaitAsync();
        _keyState[key] = true;
        _ = _keyStateSemaphore.Release();
    }

    public void SetState(BackgroundEmulatorState state)
    {
        _state = state;
    }

    public Device? GetDevice()
    {
        return _device;
    }

    public async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        // Spin waiting for the ROM to get loaded from a client
        _device = await WaitForRomLoadAsync(cancellationToken);
        if (_device == null) return;

        var sw = new Stopwatch();

        while (!cancellationToken.IsCancellationRequested)
        {
            sw.Restart();
            await _keyStateSemaphore.WaitAsync(cancellationToken);
            foreach (var kv in _keyState)
            {
                if (kv.Value)
                {
                    _device.PressKey(kv.Key);
                }
                else
                {
                    _device.ReleaseKey(kv.Key);
                }
            }
            _ = _keyStateSemaphore.Release();

            switch (_state)
            {
                case BackgroundEmulatorState.WaitingForRom:
                    _device = await WaitForRomLoadAsync(cancellationToken);
                    if (_device == null) return;
                    break;
                case BackgroundEmulatorState.Running:
                    _device.RunFrame();
                    await _emulatorHubContext.Clients.All.SendFrame(Convert.ToBase64String(_device.GetFrame())); // TODO - awaiting these might slow frames down, could we fire and forget?
                    sw.Stop();

                    var remainingMsInFrame = (int)(16.67 - sw.ElapsedMilliseconds);

                    if (remainingMsInFrame > 0)
                    {
                        await Task.Delay(remainingMsInFrame, cancellationToken);
                    }
                    break;
                case BackgroundEmulatorState.Paused:
                    await Task.Delay(100, cancellationToken);
                    break;
            }
        }
    }
}
