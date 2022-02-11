using Microsoft.CodeAnalysis;
using System;

namespace GameboyAdvanced.Arm.SourceGenerators
{
    /// <summary>
    /// This generator is responsible for creating the implementations of all
    /// the LDR/STR functions grouped under "Single Data Transfer" operations.
    /// </summary>
    /// 
    /// <remarks>
    /// Note that it is not currently responsible for generating the functions 
    /// for strh & ldrh.
    /// </remarks>
    [Generator]
    public class ArmSingleDataTransferGenerator : ISourceGenerator
    {
        private enum ShiftType
        {
            Ll,
            Lr,
            Ar,
            Rr,
            Imm,
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var fullSource = @"// Auto-generated code
namespace GameboyAdvanced.Core.Cpu;

internal static unsafe partial class Arm
{
";

            foreach (bool l in new[] { true, false })
            {
                var nRWStr = l ? "core.nRW = false;" : "core.nRW = true;";
                var nextActionStr = l ? "core.NextExecuteAction = &ldr_writeback;" : "core.NextExecuteAction = &Core.ResetMemoryUnitForOpcodeFetch;";

                foreach (var width in new[] { "b", "w" })
                {
                    var widthSetStr = width switch
                    {
                        "b" => "core.MAS = BusWidth.Byte;",
                        "w" => "core.MAS = BusWidth.Word;",
                        _ => throw new Exception(),
                    };

                    var dataSetStr = (l, width) switch
                    {
                        (true, _) => "",
                        (false, "b") => "core.D = (core.R[rd] & 0xFF) | ((core.R[rd] & 0xFF) << 8) | ((core.R[rd] & 0xFF) << 16) | ((core.R[rd] & 0xFF) << 24);",
                        (false, "w") => "core.D = core.R[rd];",
                        _ => throw new Exception(),
                    };

                    foreach (var pre in new[] { true, false })
                    {
                        foreach (var inc in new[] { true, false })
                        {
                            foreach (var writeback in new[] { true, false })
                            {
                                if (!writeback && !pre) continue; // This would just generate duplicate functions

                                var addressSetStr = (pre, inc) switch
                                {
                                    (true, true) => "core.A = core.R[rn] + offset;",
                                    (true, false) => "core.A = core.R[rn] - offset;",
                                    (false, _) => "core.A = core.R[rn];",
                                };

                                var writebackStr = (writeback, inc) switch
                                {
                                    (false, _) => "",
                                    (true, true) => "core.R[rn] = core.R[rn] + offset;",
                                    (true, false) => "core.R[rn] = core.R[rn] - offset;",
                                };

                                foreach (ShiftType shiftType in Enum.GetValues(typeof(ShiftType)))
                                {
                                    var funcName = l ? "ldr" : "str";
                                    funcName += (width == "b") ? "b_" : "_";
                                    funcName += (writeback, pre) switch
                                    {
                                        (true, true) => "pr",
                                        (true, false) => "pt",
                                        (false, _) => "of",
                                    };
                                    funcName += shiftType switch
                                    {
                                        ShiftType.Imm => "i",
                                        _ => "r",
                                    };
                                    funcName += inc ? "p" : "m";
                                    funcName += shiftType switch
                                    {
                                        ShiftType.Imm => "",
                                        _ => shiftType.ToString().ToLowerInvariant(),
                                    };

                                    var shiftedStr = shiftType switch
                                    {
                                        ShiftType.Imm => "",
                                        _ => @"
var rm = instruction & 0b1111;
var shiftType = (instruction >> 4) & 0b11;
var shiftAmount = (byte)((instruction >> 7) & 0b1_1111);",
                                    };
                                    var offsetAdjustStr = shiftedStr + (shiftType switch
                                    {
                                        ShiftType.Ll => "var offset = ALU.LSLNoFlags(core.R[rm], shiftAmount);",
                                        ShiftType.Lr => "var offset = ALU.LSRNoFlags(core.R[rm], shiftAmount);",
                                        ShiftType.Ar => "var offset = ALU.ASRNoFlags(core.R[rm], shiftAmount);",
                                        ShiftType.Rr => "var offset = ALU.RORNoFlags(core.R[rm], shiftAmount);",
                                        ShiftType.Imm => "var offset = instruction & 0b1111_1111_1111;",
                                        _ => throw new Exception(),
                                    });

                                    var func = $@"
static partial void {funcName}(Core core, uint instruction)
{{
    var rn = (instruction >> 16) & 0b1111;
    var rd = (instruction >> 12) & 0b1111;
    
    {offsetAdjustStr}

    {addressSetStr}

    {writebackStr}

    {widthSetStr}

    {dataSetStr}

    core.nOPC = true;
    core.SEQ = false;
    {nRWStr}
    {nextActionStr}
}}

";

                                    fullSource += func;
                                }
                            }
                        }
                    }
                }
            }

            fullSource += "}";

            context.AddSource("ArmLdrStrOps.g.cs", fullSource);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
