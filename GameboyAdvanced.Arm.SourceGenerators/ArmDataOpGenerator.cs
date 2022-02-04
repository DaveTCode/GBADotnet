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
namespace GameboyAdvanced.Core.Cpu;

internal static unsafe partial class Arm
{
";

            foreach (Operation op in Enum.GetValues(typeof(Operation)))
            {
                var opFunction = op switch
                {
                    Operation.And => "core.R[rd] = core.R[rn] & secondOperand;",
                    Operation.Eor => "core.R[rd] = core.R[rn] ^ secondOperand;",
                    Operation.Sub => "core.R[rd] = ALU.SUB(core.R[rn], secondOperand, ref core.Cpsr);",
                    Operation.Rsb => "core.R[rd] = ALU.SUB(secondOperand, core.R[rn], ref core.Cpsr);",
                    Operation.Add => "core.R[rd] = ALU.ADD(core.R[rn], secondOperand, ref core.Cpsr);",
                    Operation.Adc => "core.R[rd] = ALU.ADC(core.R[rn], secondOperand, ref core.Cpsr);",
                    Operation.Sbc => "core.R[rd] = ALU.SBC(core.R[rn], secondOperand, ref core.Cpsr);",
                    Operation.Rsc => "core.R[rd] = ALU.SBC(secondOperand, core.R[rn], ref core.Cpsr);",
                    Operation.Tst => "var result = core.R[rn] & secondOperand;",
                    Operation.Teq => "var result = core.R[rn] ^ secondOperand;",
                    Operation.Cmp => "var result = ALU.SUB(core.R[rn], secondOperand, ref core.Cpsr);",
                    Operation.Cmn => "var result = ALU.ADD(core.R[rn], secondOperand, ref core.Cpsr);",
                    Operation.Orr => "core.R[rd] = core.R[rn] | secondOperand;",
                    Operation.Mov => "core.R[rd] = secondOperand;",
                    Operation.Bic => "core.R[rd] = core.R[rn] & ~secondOperand;",
                    Operation.Mvn => "core.R[rd] = ~secondOperand;",
                    _ => throw new Exception($"Invalid operation during data op source generation {op}"),
                };

                foreach (var s in new[] { true, false })
                {
                    // Cmp/Cmn/Tst/Teq don't have versions which don't set flags because that would be a noop
                    if (!s && (op == Operation.Cmp || op == Operation.Cmn || op == Operation.Tst || op == Operation.Teq))
                    {
                        continue;
                    }

                    var funcName = op.ToString().ToLowerInvariant() + (s ? "s" : "");
                    var sStatement = (s, op) switch
                    {
                        (false, _) => "",
                        (true, Operation.Tst) => "ALU.SetZeroSignFlags(ref core.Cpsr, result);",
                        (true, Operation.Teq) => "ALU.SetZeroSignFlags(ref core.Cpsr, result);",
                        (true, Operation.Cmp) => "ALU.SetZeroSignFlags(ref core.Cpsr, result);",
                        (true, Operation.Cmn) => "ALU.SetZeroSignFlags(ref core.Cpsr, result);",
                        (true, _) => "ALU.SetZeroSignFlags(ref core.Cpsr, core.R[rd]);",
                    };

                    // First output the op_imm and ops_imm functions as those
                    // are sufficiently different.
                    var immSource = $@"

    static partial void {funcName}_imm(Core core, uint instruction)
    {{
        var rn = (instruction >> 16) & 0b1111;
        var rd = (instruction >> 12) & 0b1111;
        var imm = instruction & 0b1111_1111;
        var rot = ((instruction >> 8) & 0b1111) * 2;
        var secondOperand = ALU.RORNoFlags(imm, (byte)rot); // TODO - Does carry get set to output of ROR from ALU?
        
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
    }}
";

                    fullSource += immSource;

                    foreach (OperandType type in Enum.GetValues(typeof(OperandType)))
                    {
                        var secondOperandStatement = (s, type) switch
                        {
                            (false, OperandType.Lli) => "ALU.LSLNoFlags(core.R[rm], (byte)((instruction >> 7) & 0b1_1111));",
                            (true, OperandType.Lli) => "ALU.LSL(core.R[rm], (byte)((instruction >> 7) & 0b1_1111), ref core.Cpsr);",
                            (false, OperandType.Lri) => "ALU.LSRNoFlags(core.R[rm], (byte)((instruction >> 7) & 0b1_1111));",
                            (true, OperandType.Lri) => "ALU.LSR(core.R[rm], (byte)((instruction >> 7) & 0b1_1111), ref core.Cpsr);",
                            (false, OperandType.Ari) => "ALU.ASRNoFlags(core.R[rm], (byte)((instruction >> 7) & 0b1_1111));",
                            (true, OperandType.Ari) => "ALU.ASR(core.R[rm], (byte)((instruction >> 7) & 0b1_1111), ref core.Cpsr);",
                            (false, OperandType.Rri) => "ALU.RORNoFlags(core.R[rm], (byte)((instruction >> 7) & 0b1_1111));",
                            (true, OperandType.Rri) => "ALU.ROR(core.R[rm], (byte)((instruction >> 7) & 0b1_1111), ref core.Cpsr);",

                            (false, OperandType.Llr) => "ALU.LSLNoFlags(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111]);",
                            (true, OperandType.Llr) => "ALU.LSL(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111], ref core.Cpsr);",
                            (false, OperandType.Lrr) => "ALU.LSRNoFlags(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111]);",
                            (true, OperandType.Lrr) => "ALU.LSR(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111], ref core.Cpsr);",
                            (false, OperandType.Arr) => "ALU.ASRNoFlags(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111]);",
                            (true, OperandType.Arr) => "ALU.ASR(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111], ref core.Cpsr);",
                            (false, OperandType.Rrr) => "ALU.RORNoFlags(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111]);",
                            (true, OperandType.Rrr) => "ALU.ROR(core.R[rm], (byte)core.R[(instruction >> 8) & 0b1111], ref core.Cpsr);",
                            _ => throw new Exception("Invalid data operation"),
                        };

                        var waitStatesStatement = type switch
                        {
                            var t when t == OperandType.Llr || t == OperandType.Lrr || t == OperandType.Arr || t == OperandType.Rrr => @"
// Shifts by a register quantity cause a single I cycle which we represent by adding a wait state
// Note that I cycles aren't wait states, but I'm not convinced that you can tell from outside the cpu at the moment!
core.WaitStates++;",
                            _ => ""
                        };

                        var source = $@" // Auto-generated code
static partial void {funcName + "_" + type.ToString().ToLowerInvariant()}(Core core, uint instruction)
    {{
        var rn = (instruction >> 16) & 0b1111;
        var rd = (instruction >> 12) & 0b1111;

        uint secondOperand;
        var shift = (instruction >> 4) & 0b1111_1111;
        var rm = instruction & 0b1111;

        secondOperand = {secondOperandStatement}

        {waitStatesStatement}

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
    }}
";

                        fullSource += source;
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