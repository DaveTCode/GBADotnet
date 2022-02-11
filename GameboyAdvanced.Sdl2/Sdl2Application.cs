using GameboyAdvanced.Core;
using SDL2;
using System.Diagnostics;

namespace GameboyAdvanced.Sdl2;

internal class Sdl2Application : IDisposable
{
    private readonly Device _device;
    private IntPtr _window;
    private IntPtr _renderer;
    private IntPtr _texture;
    private readonly Stopwatch _stopwatch = new();
    private bool _disposedValue;
    private readonly int _msPerFrame = (int)(1.0 / 60 * 1000);

    internal Sdl2Application(Device device)
    {
        _device = device;
    }

    private void SetupSdl2()
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
        {
            throw new Exception($"Can't set up SDL video: {SDL.SDL_GetError()}");
        }

        _window = SDL.SDL_CreateWindow(
            $"GBA - {_device.LoadedRomName()}",
            SDL.SDL_WINDOWPOS_UNDEFINED,
            SDL.SDL_WINDOWPOS_UNDEFINED,
            240,
            160,
            SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

        if (_window == IntPtr.Zero)
        {
            throw new Exception($"Can't set up SDL window: {SDL.SDL_GetError()}");
        }

        _renderer = SDL.SDL_CreateRenderer(
            _window,
            -1,
            SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

        if (_renderer == IntPtr.Zero)
        {
            throw new Exception($"Can't set up SDL renderer: {SDL.SDL_GetError()}");
        }

        _texture = SDL.SDL_CreateTexture(
            _renderer,
            SDL.SDL_PIXELFORMAT_ABGR8888,
            (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
            Device.WIDTH,
            Device.HEIGHT);
    }

    internal void Run()
    {
        SetupSdl2();
        _stopwatch.Start();
        var running = true;

        while (running)
        {
            while (SDL.SDL_PollEvent(out SDL.SDL_Event e) == 1)
            {
                switch (e.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        running = false;
                        break;
                    // TODO - Not handling inputs
                }
            }

            // TODO - input latency is one frame if we clock a single frame before checking before events
            _device.RunFrame();

            var frameBuffer = _device.GetFrame();

            unsafe
            {
                fixed (byte* p = frameBuffer)
                {
                    _ = SDL.SDL_UpdateTexture(_texture, IntPtr.Zero, (IntPtr)p, Device.WIDTH * 4);
                }
            }

            _ = SDL.SDL_RenderCopy(_renderer, _texture, IntPtr.Zero, IntPtr.Zero);
            SDL.SDL_RenderPresent(_renderer);

            var msToSleep = _msPerFrame - (_stopwatch.ElapsedTicks / (double)Stopwatch.Frequency * 1000);
            Console.WriteLine(msToSleep);
            if (msToSleep > 0)
            {
                SDL.SDL_Delay((uint)msToSleep);
            }
            _stopwatch.Restart();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (_texture != IntPtr.Zero) SDL.SDL_DestroyTexture(_texture);
            if (_renderer != IntPtr.Zero) SDL.SDL_DestroyRenderer(_renderer);
            if (_window != IntPtr.Zero) SDL.SDL_DestroyWindow(_window);
            SDL.SDL_Quit();
            _disposedValue = true;
        }
    }

    ~Sdl2Application()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
