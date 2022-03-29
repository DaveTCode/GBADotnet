using GameboyAdvanced.Core;
using GameboyAdvanced.Core.Input;
using GameboyAdvanced.Web.Emulation;
using Microsoft.AspNetCore.SignalR;

namespace GameboyAdvanced.Web.Signalr;

public interface IEmulatorClient
{
    Task SendFrame(string base64EncodedFrameData);

    Task SendDevice(Device? device);
}

public class EmulatorHub : Hub<IEmulatorClient>
{
    private readonly BackgroundEmulatorThread _backgroundEmulatorThread;

    public EmulatorHub(BackgroundEmulatorThread backgroundEmulatorThread)
    {
        _backgroundEmulatorThread = backgroundEmulatorThread ?? throw new ArgumentNullException(nameof(backgroundEmulatorThread));
    }

    public async Task Pause()
    {
        _backgroundEmulatorThread.SetState(BackgroundEmulatorState.Paused);
        await Clients.All.SendDevice(_backgroundEmulatorThread.GetDevice());
    }

    public void Resume()
    {
        _backgroundEmulatorThread.SetState(BackgroundEmulatorState.Running);
    }

    public async Task KeyUp(Key key)
    {
        await _backgroundEmulatorThread.KeyUp(key);
    }

    public async Task KeyDown(Key key)
    {
        await _backgroundEmulatorThread.KeyDown(key);
    }
}
