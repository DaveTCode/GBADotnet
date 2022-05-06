using CommandLine;
using GameboyAdvanced.Core;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Rom;

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

        [Option("pixelSize", Default = 4, HelpText = "The number of pixels (squared) that represent each GBA pixel")]
        public int PixelSize { get; }

        [Option("backupType", Default = null, HelpText = "The emulator will attempt to guess the backup type but if that fails you can specify it directly with this")]
        public RomBackupType? BackupType { get; }

        [Option("unlockFps", Default = false, HelpText = "Set to true to allow > 60fps")]
        public bool UnlockFps { get; }

        public Options(string rom, string bios, bool runBios, int pixelSize, RomBackupType? backupType, bool unlockFps)
        {
            Rom = rom ?? throw new ArgumentNullException(nameof(rom));
            Bios = bios ?? throw new ArgumentNullException(nameof(bios));
            RunBios = runBios;
            PixelSize = pixelSize;
            BackupType = backupType;
            UnlockFps = unlockFps;
        }
    }

    internal static void Main(string[] args)
    {
        _ = Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                var bios = File.ReadAllBytes(o.Bios);
                var rom = File.ReadAllBytes(o.Rom);

                var gamepak = new GamePak(rom, o.BackupType);
                var device = new Device(bios, gamepak, new TestDebugger(), !o.RunBios);

                var application = new Sdl2Application(o.Rom, device, o.PixelSize, o.UnlockFps);
                application.Run();
            })
            .WithNotParsed(o =>
            {
                Console.WriteLine(o);
            });
    }
}