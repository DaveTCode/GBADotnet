using CommandLine;
using GameboyAdvanced.Core;
using GameboyAdvanced.Core.Rom;

namespace GameboyAdvanced.Console;

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
        _ = Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                var bios = File.ReadAllBytes(o.Bios);
                var rom = File.ReadAllBytes(o.Rom);

                var gamepak = new GamePak(rom);
                var device = new Device(bios, gamepak, o.RunBios ? 0x0000_0000u : 0x0800_0000u);

                var overflowCycles = 0;
                while (true)
                {
                    overflowCycles = device.RunFrame(overflowCycles);
                }
            });
    }
}