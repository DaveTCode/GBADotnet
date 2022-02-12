using CommandLine;
using GameboyAdvanced.Core;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Rom;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace GameboyAdvanced.Headless;

internal class Program
{
    private readonly static Regex NextCycleRegex = new("n ?(?<cycles>\\d+)?");
    private readonly static Regex SetBreakpointRegex = new("b (0x)?(?<address>[0-9a-fA-F]{8})");
    private readonly static Regex SetReadBreakpointRegex = new("br (0x)?(?<address>[0-9a-fA-F]{8})");
    private readonly static Regex SetWriteBreakpointRegex = new("bw (0x)?(?<address>[0-9a-fA-F]{8})(=(0x)?(?<value>[0-9a-fA-F]{8}))?");
    private readonly static Regex SetRegBreakpointRegex = new("breg [rR]?(?<reg>\\d{1,2})=(0x)?(?<value>[0-9a-fA-F]{8})");
    private readonly static Regex MemoryReadRegex = new("m[bhw] (0x)?(?<address>[0-9a-fA-F]{8})");

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
        var consoleLevelLoggingSwitch = new LoggingLevelSwitch(LogEventLevel.Debug);
        var logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}").MinimumLevel.ControlledBy(consoleLevelLoggingSwitch)
            .WriteTo.File("log.txt", outputTemplate: "{Message:lj}{NewLine}", rollOnFileSizeLimit: true, retainedFileCountLimit: 1)
            .CreateLogger();

        _ = Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                var bios = File.ReadAllBytes(o.Bios);
                var rom = File.ReadAllBytes(o.Rom);

                var gamepak = new GamePak(rom);
                var device = new Device(bios, gamepak, new Debugger(logger), o.RunBios ? 0x0000_0000u : 0x0800_0000u);

                while (true)
                {
                    Console.Write("> ");
                    var debugLine = Console.ReadLine()?.ToLowerInvariant();

                    if (string.IsNullOrWhiteSpace(debugLine)) continue;

                    switch (debugLine)
                    {
                        case var s when NextCycleRegex.IsMatch(s): // Run next cycle (ignore breakpoints)
                            {
                                var match = NextCycleRegex.Match(s);
                                var cycles = match.Groups["cycles"].Success ? int.Parse(match.Groups["cycles"].Value) : 0;

                                try
                                {
                                    for (var ii = 0; ii < cycles; ii++)
                                    {
                                        device.RunCycle(false);
                                    }
                                }
                                catch (BreakpointException) { }
                                Console.WriteLine(device.ToString());

                                break;
                            }
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
                            device.Reset(o.RunBios ? 0x0000_0000u : 0x0800_0000u);
                            break;
                        case "logsoff": // Turn off logging
                            consoleLevelLoggingSwitch.MinimumLevel = LogEventLevel.Warning;
                            break;
                        case "logson": // Turn off logging
                            consoleLevelLoggingSwitch.MinimumLevel = LogEventLevel.Debug;
                            break;
                        case var s when s.StartsWith("mw"): // Inspect word
                            {
                                var match = MemoryReadRegex.Match(debugLine);
                                Console.WriteLine(device.InspectWord(Convert.ToUInt32(match.Groups["address"].Value, 16)).ToString("X8"));
                                break;
                            }
                        case var s when s.StartsWith("mh"): // Inspect half word
                            {
                                var match = MemoryReadRegex.Match(debugLine);
                                Console.WriteLine(device.InspectHalfWord(Convert.ToUInt32(match.Groups["address"].Value, 16)).ToString("X4"));
                                break;
                            }
                        case var s when s.StartsWith("mb"): // Inspect byte
                            {
                                var match = MemoryReadRegex.Match(debugLine);
                                Console.WriteLine(device.InspectByte(Convert.ToUInt32(match.Groups["address"].Value, 16)).ToString("X2"));
                                break;
                            }
                        case var s when s.StartsWith("c"): // Run to next breakpoint
                            while (true)
                            {
                                try
                                {
                                    device.RunCycle();

                                    if (Console.KeyAvailable)
                                    {
                                        var key = Console.ReadKey(true);
                                        if (key.Key == ConsoleKey.Spacebar)
                                        {
                                            break;
                                        }
                                    }
                                }
                                catch (BreakpointException) { break; }
                            }
                            break;
                        case var s when SetBreakpointRegex.IsMatch(s): // Set code breakpoint
                            {
                                var match = SetBreakpointRegex.Match(debugLine);
                                var address = Convert.ToUInt32(match.Groups["address"].Value, 16);
                                device.Debugger.RegisterExecuteBreakpoint(address);
                                break;
                            }
                        case var s when SetReadBreakpointRegex.IsMatch(s): // Set read memory breakpoint
                            {
                                var match = SetReadBreakpointRegex.Match(debugLine);
                                if (match.Success)
                                {
                                    var address = Convert.ToUInt32(match.Groups["address"].Value, 16);
                                    device.Debugger.RegisterReadBreakpoint(address);
                                }
                                break;
                            }
                        case var s when SetWriteBreakpointRegex.IsMatch(s): // Set write memory breakpoint
                            {
                                var match = SetWriteBreakpointRegex.Match(debugLine);
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
                        case var s when SetRegBreakpointRegex.IsMatch(s): // Set write memory breakpoint
                            {
                                var match = SetRegBreakpointRegex.Match(debugLine);
                                if (match.Success)
                                {
                                    var reg = Convert.ToInt32(match.Groups["reg"].Value);
                                    var val = Convert.ToUInt32(match.Groups["value"].Value, 16);

                                    device.Debugger.RegisterRegValBreakpoint(reg, val);
                                }
                                break;
                            }
                        case "fb": // Dump framebuffer to a file
                            {
                                var lines = device.GetFrame()
                                    .Select((b, ix) => new { b, ix })
                                    .GroupBy(x => x.ix / Device.WIDTH)
                                    .Select(g => string.Join(",", g.Select(x => x.b.ToString("X2"))));
                                File.WriteAllText("frame_buffer.csv", string.Join("\n", lines));
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