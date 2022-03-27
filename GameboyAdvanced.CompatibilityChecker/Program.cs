﻿using CommandLine;
using GameboyAdvanced.Core;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Rom;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;

namespace GameboyAdvanced.Sdl2;

internal class Program
{
    public class Options
    {
        [Option('r', "romDirectory", Required = true)]
        public string RomDirectory { get; }

        [Option('o', "outputDirectory", Required = true)]
        public string OutputDirectory { get; }

        [Option('b', "bios", Required = true)]
        public string Bios { get; }

        public Options(string romDirectory, string bios, string outputDirectory)
        {
            RomDirectory = romDirectory ?? throw new ArgumentNullException(nameof(romDirectory));
            Bios = bios ?? throw new ArgumentNullException(nameof(bios));
            OutputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
        }
    }

    internal static void Main(string[] args)
    {
        _ = Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                var bios = File.ReadAllBytes(o.Bios);
                var compatibilityDatabaseContents = new StringBuilder();

                _ = compatibilityDatabaseContents
                    .AppendLine("# Autogenerated Compatibility")
                    .AppendLine("")
                    .AppendLine("This file contains a table with all ROMS tested along with some details parsed from the ROM and a screenshot from 100 frames in with no button presses")
                    .AppendLine("")
                    .AppendLine("<table>")
                    .AppendLine("  <thead>")
                    .AppendLine("    <tr>")
                    .Append("<td>Filename</td>")
                    .Append("<td>Game Title</td>")
                    .Append("<td>Bootable</td>")
                    .Append("<td>Save Type</td>")
                    .Append("<td>Image</td>")
                    .AppendLine("    </tr>")
                    .AppendLine("  </thead>")
                    .AppendLine("  <tbody>");

                _ = Directory.CreateDirectory(Path.Join(o.OutputDirectory, "images"));

                foreach (var romFile in Directory.EnumerateFiles(o.RomDirectory, "*.gba"))
                {
                    var romFilename = Path.GetFileName(romFile);
                    var rom = File.ReadAllBytes(romFile);
                    var gamepak = new GamePak(rom);
                    var device = new Device(bios, gamepak, new TestDebugger(), true);

                    var bootable = ":heavy_check_mark:";
                    try
                    {
                        for (var ii = 0; ii < 500; ii++)
                        {
                            device.RunFrame();
                        }
                    }
                    catch (Exception ex)
                    {
                        bootable = $":x: - {ex.Message}";
                    }

                    var frameBuffer = device.GetFrame();

                    var outputFilePath = Path.Join(o.OutputDirectory, "images", romFilename.Replace(".gba", ".png"));
                    using var outputFile = File.Create(outputFilePath);
                    Image.LoadPixelData<Rgba32>(frameBuffer, Device.WIDTH, Device.HEIGHT).Save(outputFile, new PngEncoder());

                    var imgLink = bootable == ":x:" 
                        ? ""
                        : $"<img src=\"./images/{romFilename.Replace(".gba", ".png")}\" alt=\"{romFilename.Replace(".gba", "")}\"></img>";

                    _ = compatibilityDatabaseContents
                        .Append("<tr>")
                        .Append($"<td>{romFilename.Replace(".gba", "")}</td>")
                        .Append($"<td>{device.Gamepak.GameTitle}</td>")
                        .Append($"<td>{bootable}</td>")
                        .Append($"<td>{device.Gamepak.RomBackupType}</td>")
                        .Append($"<td>{imgLink}</td>")
                        .AppendLine("</tr>");
                }

                _ = compatibilityDatabaseContents.AppendLine("</table>");
                File.WriteAllText(Path.Join(o.OutputDirectory, "readme.md"), compatibilityDatabaseContents.ToString());
            })
            .WithNotParsed(o =>
            {
                Console.WriteLine(o);
            });
    }
}