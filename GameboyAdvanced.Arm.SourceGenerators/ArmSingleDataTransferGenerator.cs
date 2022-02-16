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
using static GameboyAdvanced.Core.Cpu.ALU;
using static GameboyAdvanced.Core.Cpu.Shifter;
using GameboyAdvanced.Core.Cpu.Shared;
namespace GameboyAdvanced.Core.Cpu;

internal static unsafe partial class Arm
{
";

            foreach (bool l in new[] { true, false })
            {
                foreach (var byteWide in new[] { true, false })
                {
                    foreach (var pre in new[] { true, false })
                    {
                        foreach (var inc in new[] { true, false })
                        {
                            foreach (var writeback in new[] { true, false })
                            {
                                if (!writeback && !pre) continue; // This would just generate duplicate functions

                                var width = byteWide ? "BusWidth.Byte" : "BusWidth.Word";

                                var dataStr = (l, byteWide) switch
                                {
                                    (true, _) => "",
                                    (false, true) => "(core.R[rd] & 0xFF) | ((core.R[rd] & 0xFF) << 8) | ((core.R[rd] & 0xFF) << 16) | ((core.R[rd] & 0xFF) << 24)",
                                    (false, false) => "core.R[rd]",
                                };

                                var addressStr = (pre, inc) switch
                                {
                                    (true, true) => "core.R[rn] + offset",
                                    (true, false) => "core.R[rn] - offset",
                                    (false, _) => "core.R[rn]",
                                };

                                var writebackVal = (writeback, inc) switch
                                {
                                    (false, _) => "",
                                    (true, true) => "core.R[rn] + offset",
                                    (true, false) => "core.R[rn] - offset",
                                };

                                foreach (ShiftType shiftType in Enum.GetValues(typeof(ShiftType)))
                                {
                                    var funcName = l ? "ldr" : "str";
                                    funcName += byteWide ? "b_" : "_";
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
                                        ShiftType.Ll => "var offset = LSLNoFlags(core.R[rm], shiftAmount);",
                                        ShiftType.Lr => "var offset = LSRImmediateNoFlags(core.R[rm], shiftAmount);",
                                        ShiftType.Ar => "var offset = ASRImmediateNoFlags(core.R[rm], shiftAmount);",
                                        ShiftType.Rr => "var offset = RORRegisterNoFlags(core.R[rm], shiftAmount);",
                                        ShiftType.Imm => "var offset = instruction & 0b1111_1111_1111;",
                                        _ => throw new Exception(),
                                    });
                                    var castFunc = byteWide ? "&LdrStrUtils.LDRB" : "&LdrStrUtils.LDRW";

                                    var common = (l, writeback) switch
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
    
    {offsetAdjustStr}

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

            fullSource += "}";

            context.AddSource("ArmLdrStrOps.g.cs", fullSource);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
