using CommandLine;
using GameboyAdvanced.Core;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Rom;
using Serilog;

namespace GameboyAdvanced.Sdl2;

internal class Program
{
    public class Options
    {
        [Option('r', "rom", Required = true)]
        public string Rom { get; }

        [Option('b', "bios", Required = true)]
        public string Bios { get; }

        [Option("runBios", Default = false, HelpText = "Set --runBios to force the device to start at the RESET vector (0x0000_0000) instead of the first instruction on the gamepak")]
        public bool RunBios { get; }

        public Options(string rom, string bios, bool runBios)
        {
            Rom = rom ?? throw new ArgumentNullException(nameof(rom));
            Bios = bios ?? throw new ArgumentNullException(nameof(bios));
            RunBios = runBios;
        }
    }

    internal static void Main(string[] args)
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            //.WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}")
            .WriteTo.File("log.txt", outputTemplate: "{Message:lj}{NewLine}")
            .CreateLogger();

        _ = Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                var bios = File.ReadAllBytes(o.Bios);
                var rom = File.ReadAllBytes(o.Rom);

                var gamepak = new GamePak(rom);
                var device = new Device(bios, gamepak, new TestDebugger(), o.RunBios ? 0x0000_0000u : 0x0800_0000u);

                var application = new Sdl2Application(device);
                application.Run();
            });
    }
}