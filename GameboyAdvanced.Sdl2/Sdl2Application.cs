using GameboyAdvanced.Core;
using GameboyAdvanced.Core.Input;
using GameboyAdvanced.Core.Ppu;
using SDL2;
using Serilog.Core;
using System.Diagnostics;
using System.Security.Cryptography;

namespace GameboyAdvanced.Sdl2;

internal class Sdl2Application : IDisposable
{
    private readonly string _originalFilePath;
    private readonly Device _device;
    private readonly int _pixelSize;
    private IntPtr _window;
    private IntPtr _renderer;
    private IntPtr _texture;
    private bool _disposedValue;
    private readonly int _msPerFrame = (int)(1.0 / 60 * 1000);
    private float _framesPerSecond;

    private readonly Dictionary<SDL.SDL_Keycode, Key> _keyMap = new()
    {
        { SDL.SDL_Keycode.SDLK_z, Key.A },
        { SDL.SDL_Keycode.SDLK_x, Key.B },
        { SDL.SDL_Keycode.SDLK_UP, Key.Up },
        { SDL.SDL_Keycode.SDLK_DOWN, Key.Down },
        { SDL.SDL_Keycode.SDLK_LEFT, Key.Left },
        { SDL.SDL_Keycode.SDLK_RIGHT, Key.Right },
        { SDL.SDL_Keycode.SDLK_a, Key.L },
        { SDL.SDL_Keycode.SDLK_s, Key.R },
        { SDL.SDL_Keycode.SDLK_q, Key.Select },
        { SDL.SDL_Keycode.SDLK_w, Key.Start },
    };

    internal Sdl2Application(string originalFilePath, Device device, int pixelSize)
    {
        _originalFilePath = originalFilePath;
        _device = device;
        _pixelSize = pixelSize;
    }

    private void SetupSdl2()
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
        {
            throw new Exception($"Can't set up SDL video: {SDL.SDL_GetError()}");
        }

        _window = SDL.SDL_CreateWindow(
            $"GBA - {_device.Gamepak}",
            SDL.SDL_WINDOWPOS_UNDEFINED,
            SDL.SDL_WINDOWPOS_UNDEFINED,
            Device.WIDTH * _pixelSize,
            Device.HEIGHT * _pixelSize,
            SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
        SDL.SDL_SetWindowMinimumSize(_window, Device.WIDTH, Device.HEIGHT);

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
        var secondStopwatch = new Stopwatch();
        var frameTimeStopwatch = new Stopwatch();
        var adjustStopwatch = new Stopwatch();
        secondStopwatch.Start();
        frameTimeStopwatch.Start();
        adjustStopwatch.Start();
        var adjustTicks = 0L;
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
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                        if (_keyMap.TryGetValue(e.key.keysym.sym, out var dKey))
                        {
                            _device.PressKey(dKey);
                        }

                        if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_F2)
                        {
                            var frame = _device.GetFrame();
                            using MD5 md5 = MD5.Create();
                            byte[] hashBytes = md5.ComputeHash(frame);

                            Console.WriteLine($"[InlineData(@\"{_originalFilePath}\",\"{Convert.ToHexString(hashBytes)}\",{_device.Cpu.Cycles / Ppu.FrameCycles})]");
                        }
                        else if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_F3)
                        {
                            for (var ii = 0; ii <= 0xFF; ii++)
                            {
                                Console.WriteLine($"{ii:D3}: {_device.InstructionBuffer[(byte)(_device.InstructionBufferPtr + ii)]:X8}");
                            }
                        }
                        break;
                    case SDL.SDL_EventType.SDL_KEYUP:
                        if (_keyMap.TryGetValue(e.key.keysym.sym, out var uKey))
                        {
                            _device.ReleaseKey(uKey);
                        }
                        break;
                }
            }

            // TODO - input latency is one frame if we clock a single frame before checking before events
            try
            {
                _device.RunFrame();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }

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

            var msToSleep = _msPerFrame - ((frameTimeStopwatch.ElapsedTicks + adjustTicks) / (double)Stopwatch.Frequency * 1000);
            adjustStopwatch.Restart();
            if (msToSleep > 0)
            {
                SDL.SDL_Delay((uint)msToSleep);
            }
            adjustTicks = adjustStopwatch.ElapsedTicks;
            frameTimeStopwatch.Restart();

            if (secondStopwatch.ElapsedMilliseconds >= 10000)
            {
                SDL.SDL_SetWindowTitle(_window, $"GBA - {_device.Gamepak} - {_framesPerSecond / 10f} FPS");
                _framesPerSecond = 0;
                secondStopwatch.Restart();
            }
            else
            {
                _framesPerSecond += 1;
            }

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
