using CommandLine;
using GameboyAdvanced.Core;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Rom;
using Serilog;
using Serilog.Core;
using System;
using System.Text.RegularExpressions;

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
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}")
            .WriteTo.File("log.txt", outputTemplate: "{Message:lj}{NewLine}")
            .CreateLogger();

        var setBreakpointRegex = new Regex("b (0x)?(?<address>[0-9a-fA-F]{8})");
        var setReadBreakpointRegex = new Regex("br (0x)?(?<address>[0-9a-fA-F]{8})");
        var setWriteBreakpointRegex = new Regex("bw (0x)?(?<address>[0-9a-fA-F]{8})(=(0x)?(?<value>[0-9a-fA-F]{8}))?");
        var setRegBreakpointRegex = new Regex("breg [rR]?(?<reg>\\d{1,2})=(0x)?(?<value>[0-9a-fA-F]{8})");
        var memoryReadRegex = new Regex("m[bhw] (0x)?(?<address>[0-9a-fA-F]{8})");

        _ = Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                var bios = File.ReadAllBytes(o.Bios);
                var rom = File.ReadAllBytes(o.Rom);

                var gamepak = new GamePak(rom);
                var device = new Device(bios, gamepak, new Debugger(logger), o.RunBios ? 0x0000_0000u : 0x0800_0000u);

                while (true)
                {
                    System.Console.Write("> ");
                    var debugLine = System.Console.ReadLine()?.ToLowerInvariant();

                    if (string.IsNullOrWhiteSpace(debugLine)) continue;

                    switch (debugLine)
                    {
                        case "n": // Run next cycle (ignore breakpoints)
                            try
                            {
                                device.RunCycle(false);
                            }
                            catch (BreakpointException) { }
                            System.Console.WriteLine(device.ToString());

                            break;
                        case "i": // Run the current instruction through to completion
                            device.Debugger.BreakOnNextInstruction();
                            while (true)
                            {
                                try
                                {
                                    device.RunCycle();
                                }
                                catch (BreakpointException) { break; }
                            }
                            break;
                        case "r": // Reset execution
                            device.Reset();
                            break;
                        case var s when s.StartsWith("mw"): // Inspect word
                            {
                                var match = memoryReadRegex.Match(debugLine);
                                System.Console.WriteLine(device.InspectWord(Convert.ToUInt32(match.Groups["address"].Value, 16)).ToString("X8"));
                                break;
                            }
                        case var s when s.StartsWith("mh"): // Inspect half word
                            {
                                var match = memoryReadRegex.Match(debugLine);
                                System.Console.WriteLine(device.InspectHalfWord(Convert.ToUInt32(match.Groups["address"].Value, 16)).ToString("X4"));
                                break;
                            }
                        case var s when s.StartsWith("mb"): // Inspect byte
                            {
                                var match = memoryReadRegex.Match(debugLine);
                                System.Console.WriteLine(device.InspectByte(Convert.ToUInt32(match.Groups["address"].Value, 16)).ToString("X2"));
                                break;
                            }
                        case "c": // Run to next breakpoint
                            while (true)
                            {
                                try
                                {
                                    device.RunCycle();
                                }
                                catch (BreakpointException) { break; }
                            }
                            break;
                        case var s when setBreakpointRegex.IsMatch(s): // Set code breakpoint
                            {
                                var match = setBreakpointRegex.Match(debugLine);
                                var address = Convert.ToUInt32(match.Groups["address"].Value, 16);
                                device.Debugger.RegisterExecuteBreakpoint(address);
                                break;
                            }
                        case var s when setReadBreakpointRegex.IsMatch(s): // Set read memory breakpoint
                            {
                                var match = setReadBreakpointRegex.Match(debugLine);
                                if (match.Success)
                                {
                                    var address = Convert.ToUInt32(match.Groups["address"].Value, 16);
                                    device.Debugger.RegisterReadBreakpoint(address);
                                }
                                break;
                            }
                        case var s when setWriteBreakpointRegex.IsMatch(s): // Set write memory breakpoint
                            {
                                var match = setWriteBreakpointRegex.Match(debugLine);
                                if (match.Success)
                                {
                                    var address = Convert.ToUInt32(match.Groups["address"].Value, 16);
                                    if (match.Groups.ContainsKey("value"))
                                    {
                                        device.Debugger.RegisterWriteBreakpoint(address, Convert.ToUInt32(match.Groups["value"].Value, 16));
                                    }
                                    else
                                    {
                                        device.Debugger.RegisterWriteBreakpoint(address, null);
                                    }
                                }
                                break;
                            }
                        case var s when setRegBreakpointRegex.IsMatch(s): // Set write memory breakpoint
                            {
                                var match = setRegBreakpointRegex.Match(debugLine);
                                if (match.Success)
                                {
                                    var reg = Convert.ToInt32(match.Groups["reg"].Value);
                                    var val = Convert.ToUInt32(match.Groups["value"].Value, 16);

                                    device.Debugger.RegisterRegValBreakpoint(reg, val);
                                }
                                break;
                            }
                    }
                    try
                    {
                        device.RunCycle();
                    }
                    catch (BreakpointException)
                    {

                    }
                }
            });
    }
}