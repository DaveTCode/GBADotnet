using GameboyAdvanced.Core;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Input;
using GameboyAdvanced.Core.Rom;
using GameboyAdvanced.Web.Signalr;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace GameboyAdvanced.Web.Emulation;

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

    private readonly SemaphoreSlim _pauseStateSempahore = new(1);
    private bool _paused;

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
            _logger.LogError("Can't acquire rom semaphore");
        }

        _gamePak = new GamePak(rom);

        _ = _romSemaphore.Release();
        await Task.Delay(5000);
    }

    private async Task<Device?> WaitForRomLoadAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var acquiredSemaphore = await _romSemaphore.WaitAsync(5000, cancellationToken);
            if (!acquiredSemaphore)
            {
                _logger.LogError("Can't acquire rom semaphore");
            }

            if (_gamePak != null) return new Device(_bios, _gamePak, new TestDebugger(), true);

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

    public async Task SetPause(bool pause)
    {
        await _pauseStateSempahore.WaitAsync();
        _paused = pause;
        _ = _pauseStateSempahore.Release();
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

            await _pauseStateSempahore.WaitAsync(cancellationToken);
            if (_paused)
            {
                await Task.Delay(100, cancellationToken);
            }
            else
            {
                _device.RunFrame();
                await _emulatorHubContext.Clients.All.SendFrame(Convert.ToBase64String(_device.GetFrame())); // TODO - awaiting these might slow frames down, could we fire and forget?
                sw.Stop();

                var remainingMsInFrame = (int)(16.67 - sw.ElapsedMilliseconds);

                if (remainingMsInFrame > 0)
                {
                    await Task.Delay(remainingMsInFrame, cancellationToken);
                }
            }
            _ = _pauseStateSempahore.Release();
        }
    }
}
