using Microsoft.CodeAnalysis;
using System;

namespace GameboyAdvanced.Arm.SourceGenerators
{
    /// <summary>
    /// The Armv4 instruction set provides a set of what are called "data operations"
    /// that engage the ALU.
    /// 
    /// These instructions differ slightly in:
    /// 1. Operation (AND, OR, XOR, MOV)
    /// 2. Whether they set flags
    /// 3. Their operand (immediate value, register value)
    /// 4. How the operand is transformed (shift, rotate)
    /// 
    /// This can all be handled in a single function with switch/if statements,
    /// however to avoid branching we utilise a source generator which
    /// creates these functions at compile time.
    /// </summary>
    /// 
    /// <remarks>
    /// I'll withold judgement on whether this is a good idea, whilst it's 
    /// theroetically true that it should provide a performance benefit I 
    /// haven't yet got around to benchmarking it so it could even be slower.
    /// </remarks>
    [Generator]
    public class ArmDataOpGenerator : ISourceGenerator
    {
        private enum Operation
        {
            And,
            Eor,
            Sub,
            Rsb,
            Add,
            Adc,
            Sbc,
            Rsc,
            Tst,
            Teq,
            Cmp,
            Cmn,
            Orr,
            Mov,
            Bic,
            Mvn,
        }

        private enum OperandType
        {
            Lli,
            Llr,
            Lri,
            Lrr,
            Ari,
            Arr,
            Rri,
            Rrr,
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var fullSource = @"// Auto-generated code
using static GameboyAdvanced.Core.Cpu.ALU;
using static GameboyAdvanced.Core.Cpu.Shifter;
namespace GameboyAdvanced.Core.Cpu;

internal static unsafe partial class Arm
{
";

            foreach (Operation op in Enum.GetValues(typeof(Operation)))
            {
                foreach (var s in new[] { true, false })
                {
                    // Cmp/Cmn/Tst/Teq don't have versions which don't set flags because that would be a noop
                    if (!s && (op == Operation.Cmp || op == Operation.Cmn || op == Operation.Tst || op == Operation.Teq))
                    {
                        continue;
                    }

                    var opFunction = (op, s) switch
                    {
                        (Operation.And, _) => "core.R[rd] = core.R[rn] & secondOperand;",
                        (Operation.Eor, _) => "core.R[rd] = core.R[rn] ^ secondOperand;",
                        (Operation.Sub, true) => "core.R[rd] = SUB(core.R[rn], secondOperand, ref core.Cpsr);",
                        (Operation.Sub, false) => "core.R[rd] = (uint)(core.R[rn] - secondOperand);",
                        (Operation.Rsb, true) => "core.R[rd] = SUB(secondOperand, core.R[rn], ref core.Cpsr);",
                        (Operation.Rsb, false) => "core.R[rd] = (uint)(secondOperand - core.R[rn]);",
                        (Operation.Add, true) => "core.R[rd] = ADD(core.R[rn], secondOperand, ref core.Cpsr);",
                        (Operation.Add, false) => "core.R[rd] = (uint)(core.R[rn] + secondOperand);",
                        (Operation.Adc, true) => "core.R[rd] = ADC(core.R[rn], secondOperand, ref core.Cpsr);",
                        (Operation.Adc, false) => "core.R[rd] = (uint)(core.R[rn] + secondOperand + (core.Cpsr.CarryFlag ? 1 : 0));",
                        (Operation.Sbc, true) => "core.R[rd] = SBC(core.R[rn], secondOperand, ref core.Cpsr);",
                        (Operation.Sbc, false) => "core.R[rd] = (uint)(core.R[rn] - secondOperand - (core.Cpsr.CarryFlag ? 0 : 1));",
                        (Operation.Rsc, true) => "core.R[rd] = SBC(secondOperand, core.R[rn], ref core.Cpsr);",
                        (Operation.Rsc, false) => "core.R[rd] = (uint)(secondOperand - core.R[rn] - (core.Cpsr.CarryFlag ? 0 : 1));",
                        (Operation.Tst, _) => "var result = core.R[rn] & secondOperand;",
                        (Operation.Teq, _) => "var result = core.R[rn] ^ secondOperand;",
                        (Operation.Cmp, _) => "var result = SUB(core.R[rn], secondOperand, ref core.Cpsr);",
                        (Operation.Cmn, _) => "var result = ADD(core.R[rn], secondOperand, ref core.Cpsr);",
                        (Operation.Orr, _) => "core.R[rd] = core.R[rn] | secondOperand;",
                        (Operation.Mov, _) => "core.R[rd] = secondOperand;",
                        (Operation.Bic, _) => "core.R[rd] = core.R[rn] & ~secondOperand;",
                        (Operation.Mvn, _) => "core.R[rd] = ~secondOperand;",
                        _ => throw new Exception($"Invalid operation during data op source generation {op}"),
                    };

                    var funcName = op.ToString().ToLowerInvariant() + (s ? "s" : "");
                    var sStatement = (s, op) switch
                    {
                        (false, _) => "",
                        (true, Operation.Tst) => "SetZeroSignFlags(ref core.Cpsr, result);",
                        (true, Operation.Teq) => "SetZeroSignFlags(ref core.Cpsr, result);",
                        (true, Operation.Cmp) => "SetZeroSignFlags(ref core.Cpsr, result);",
                        (true, Operation.Cmn) => "SetZeroSignFlags(ref core.Cpsr, result);",
                        (true, _) => "SetZeroSignFlags(ref core.Cpsr, core.R[rd]);",
                    };

                    var immSecondOperand = s
                        ? "var secondOperand = ROR(imm, (byte)rot, ref core.Cpsr);"
                        : "var secondOperand = RORInternal(imm, (byte)rot);";

                    // First output the op_imm and ops_imm functions as those
                    // are sufficiently different.
                    var immSource = $@"

    static partial void {funcName}_imm(Core core, uint instruction)
    {{
        var rn = (instruction >> 16) & 0b1111;
        var rd = (instruction >> 12) & 0b1111;
        var imm = instruction & 0b1111_1111;
        var rot = ((instruction >> 8) & 0b1111) * 2;
        {immSecondOperand}
        
        {opFunction}

        {sStatement} 

        if (rd == 15)
        {{
            core.ClearPipeline();
        }}

        core.MoveExecutePipelineToNextInstruction();
    }}
";

                    fullSource += immSource;

                    foreach (OperandType type in Enum.GetValues(typeof(OperandType)))
                    {
                        var secondOperandStatement = (s, type) switch
                        {
                            (false, OperandType.Lli) => "LSLNoFlags(core.R[rm], (byte)((instruction >> 7) & 0b1_1111));",
                            (true, OperandType.Lli) => "LSL(core.R[rm], (byte)((instruction >> 7) & 0b1_1111), ref core.Cpsr);",
                            (false, OperandType.Lri) => "LSRImmediateNoFlags(core.R[rm], (byte)((instruction >> 7) & 0b1_1111));",
                            (true, OperandType.Lri) => "LSRImmediate(core.R[rm], (byte)((instruction >> 7) & 0b1_1111), ref core.Cpsr);",
                            (false, OperandType.Ari) => "ASRImmediateNoFlags(core.R[rm], (byte)((instruction >> 7) & 0b1_1111));",
                            (true, OperandType.Ari) => "ASRImmediate(core.R[rm], (byte)((instruction >> 7) & 0b1_1111), ref core.Cpsr);",
                            (false, OperandType.Rri) => "RORNoFlagsIncRRX(core.R[rm], (byte)((instruction >> 7) & 0b1_1111), ref core.Cpsr);",
                            (true, OperandType.Rri) => "RORIncRRX(core.R[rm], (byte)((instruction >> 7) & 0b1_1111), ref core.Cpsr);",

                            (false, OperandType.Llr) => "LSLNoFlags(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111]);",
                            (true, OperandType.Llr) => "LSL(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111], ref core.Cpsr);",
                            (false, OperandType.Lrr) => "LSRRegisterNoFlags(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111]);",
                            (true, OperandType.Lrr) => "LSRRegister(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111], ref core.Cpsr);",
                            (false, OperandType.Arr) => "ASRRegisterNoFlags(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111]);",
                            (true, OperandType.Arr) => "ASRRegister(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111], ref core.Cpsr);",
                            (false, OperandType.Rrr) => "RORNoFlags(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111]);",
                            (true, OperandType.Rrr) => "ROR(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111], ref core.Cpsr);",
                            _ => throw new Exception("Invalid data operation"),
                        };

                        // Most data ops take 1S cycle to operate but operations which use a register shifted
                        // value as the second operand take 1S + 1I cycle.
                        var secondCycleFunc = $@"
static partial void {funcName + "_" + type.ToString().ToLowerInvariant()}_write(Core core, uint instruction)
{{
        var rn = (instruction >> 16) & 0b1111;
        var rd = (instruction >> 12) & 0b1111;
        var rm = instruction & 0b1111;

        var secondOperand = {secondOperandStatement}

        {opFunction}

        {sStatement} 

        // Writes to PC cause 2 extra cycles as the pipeline is flushed, note
        // though that the 2 extra cycles aren't specific to the instruction
        // they're an artifact of the flushed pipeline so don't get treated
        // here as wait states
        if (rd == 15)
        {{
            core.ClearPipeline();
        }}

        Core.ResetMemoryUnitForOpcodeFetch(core, instruction);
}}
";
                        var nextActionStatement = type == OperandType.Llr || type == OperandType.Lrr || type == OperandType.Arr || type == OperandType.Rrr
                            ? $@"
core.nMREQ = true;
core.nOPC = true;
core.NextExecuteAction = &{funcName + "_" + type.ToString().ToLowerInvariant()}_write;"
                            : $"{funcName + "_" + type.ToString().ToLowerInvariant()}_write(core, instruction);";

                        var source = $@" // Auto-generated code
static partial void {funcName + "_" + type.ToString().ToLowerInvariant()}(Core core, uint instruction)
    {{
        {nextActionStatement}
    }}
";

                        fullSource += secondCycleFunc + source;
                    }
                }
            }

            fullSource += "}";
            context.AddSource("ArmDataOps.g.cs", fullSource);
        }

        public void Initialize(GeneratorInitializationContext context)
        {

        }
    }
}