using GameboyAdvanced.Core;
using GameboyAdvanced.Core.Debug;
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

            if (_gamePak != null) return new Device(_bios, _gamePak, new TestDebugger());

            _ = _romSemaphore.Release();
            await Task.Delay(5000, cancellationToken);
        }

        return null;
    }

    public async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        // Spin waiting for the ROM to get loaded from a client
        var device = await WaitForRomLoadAsync(cancellationToken);
        if (device == null) return;

        var sw = new Stopwatch();

        while(!cancellationToken.IsCancellationRequested)
        {
            sw.Restart();
            device.RunFrame();
            await _emulatorHubContext.Clients.All.SendFrame(device.GetFrame()); // TODO - awaiting these might slow frames down, could we fire and forget?
            sw.Stop();

            var remainingMsInFrame = (int)(16.67 - sw.ElapsedMilliseconds);

            if (remainingMsInFrame > 0)
            {
                await Task.Delay(remainingMsInFrame, cancellationToken);
            }
        }
    }
}
