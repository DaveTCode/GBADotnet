using Microsoft.CodeAnalysis;

namespace GameboyAdvanced.Arm.SourceGenerators
{
    [Generator]
    internal class ArmHalfWordStoreLoadGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var fullSource = @"// Auto-generated code
using GameboyAdvanced.Core.Cpu.Shared;
namespace GameboyAdvanced.Core.Cpu;

internal static unsafe partial class Arm
{
";

            foreach (var load in new[] { true, false })
            {
                foreach (var pre in new[] { true, false })
                {
                    foreach (var inc in new[] { true, false })
                    {
                        foreach (var writeback in new[] { true, false })
                        {
                            foreach (var signed in new[] { true, false })
                            {
                                foreach (var halfword in new[] { true, false })
                                {
                                    foreach (var imm in new[] { true, false })
                                    {
                                        // Handle invalid cases e.g. "store signed byte" by skipping early
                                        if (signed && !load) continue;
                                        if (!writeback && !pre) continue;
                                        if (!signed && !halfword) continue; // Handled by single data transfer

                                        var funcName = load ? "ldr" : "str";
                                        funcName += signed ? "s" : "";
                                        funcName += halfword ? "h" : "b";
                                        funcName += (writeback, pre) switch
                                        {
                                            (true, true) => "_pr",
                                            (true, false) => "_pt",
                                            (false, _) => "_of",
                                        };
                                        funcName += (imm, inc) switch
                                        {
                                            (true, true) => "ip",
                                            (true, false) => "im",
                                            (false, true) => "rp",
                                            (false, false) => "rm",
                                        };

                                        var width = halfword ? "BusWidth.HalfWord" : "BusWidth.Byte";
                                        var castFunc = (halfword, signed) switch
                                        {
                                            (true, true) => "&LdrStrUtils.LDRSHW",
                                            (true, false) => "&LdrStrUtils.LDRHW",
                                            (false, true) => "&LdrStrUtils.LDRSB",
                                            (false, false) => "&LdrStrUtils.LDRB",
                                        };
                                        var dataStr = (load, halfword) switch
                                        {
                                            (true, _) => "",
                                            (false, false) => "(core.R[rd] & 0xFF) | ((core.R[rd] & 0xFF) << 8) | ((core.R[rd] & 0xFF) << 16) | ((core.R[rd] & 0xFF) << 24)",
                                            (false, true) => "core.R[rd] & 0xFFFF | ((core.R[rd] & 0xFFFF) << 16)",
                                        };
                                        var addressStr = (pre, inc) switch
                                        {
                                            (true, true) => "core.R[rn] + offset",
                                            (true, false) => "core.R[rn] - offset",
                                            (false, _) => "core.R[rn]",
                                        };
                                        var offsetStr = imm ?
                                            "var offset = instruction & 0b1111 | ((instruction & 0b1111_0000_0000) >> 4);" :
                                            "var offset = core.R[instruction & 0b1111];";
                                        var writebackVal = (writeback, inc) switch
                                        {
                                            (false, _) => "",
                                            (true, true) => "core.R[rn] + offset",
                                            (true, false) => "core.R[rn] - offset",
                                        };

                                        var common = (load, writeback) switch
                                        {
                                            (true, true) => $"LdrStrUtils.LDRCommonWriteback(core, {addressStr}, {width}, (int)rd, {castFunc}, (int)rn, {writebackVal});",
                                            (true, false) => $"LdrStrUtils.LDRCommon(core, {addressStr}, {width}, (int)rd, {castFunc});",
                                            (false, true) => $"LdrStrUtils.STRCommonWriteback(core, {addressStr}, {dataStr}, {width}, (int)rn, {writebackVal});",
                                            (false, false) => $"LdrStrUtils.STRCommon(core, {addressStr}, {dataStr}, {width});"
                                        };

                                        var func = $@"
static partial void {funcName}(Core core, uint instruction)
{{
    var rn = (instruction >> 16) & 0b1111;
    var rd = (instruction >> 12) & 0b1111;
    
    {offsetStr}

    {common}
}}

";
                                        fullSource += func;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            fullSource += "}";

            context.AddSource("ArmLdrhStrhOps.g.cs", fullSource);
        }

        public void Initialize(GeneratorInitializationContext context) { }
    }
}
