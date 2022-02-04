using Microsoft.AspNetCore.SignalR;

namespace GameboyAdvanced.Web.Signalr;

public interface IEmulatorClient
{
    Task SendFrame(byte[] frameData);
}

public class EmulatorHub : Hub<IEmulatorClient>
{
    public Task SendPause()
    {
        throw new NotImplementedException();
    }
}
