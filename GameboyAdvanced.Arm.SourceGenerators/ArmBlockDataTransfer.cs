using Microsoft.CodeAnalysis;
using System.Diagnostics;

namespace GameboyAdvanced.Arm.SourceGenerators
{
    [Generator]
    internal class ArmBlockDataTransfer : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var fullSource = @"// Auto-generated code
namespace GameboyAdvanced.Core.Cpu;

internal static unsafe partial class Arm
{
";

            foreach (var load in new[] { true, false })
            {
                foreach (var up in new[] { true, false })
                {
                    foreach (var pre in new[] { true, false })
                    {
                        foreach (var writeback in new[] { true, false })
                        {
                            //if (!writeback && !pre) continue;

                            foreach (var userMode in new[] { true, false })
                            {
                                var funcName = load ? "ldm" : "stm";
                                funcName += (up, pre) switch
                                {
                                    (true, true) => "ib",
                                    (true, false) => "ia",
                                    (false, true) => "db",
                                    (false, false) => "da"
                                };
                                funcName += (userMode, writeback) switch
                                {
                                    (true, true) => "_uw",
                                    (true, false) => "_u",
                                    (false, true) => "_w",
                                    (false, false) => "",
                                };

                                var writebackStr = writeback ? "_storeLoadMultipleDoWriteback = true;" : "_storeLoadMultipleDoWriteback = false;";
                                var nrwStr = load ? "core.nRW = false;" : "core.nRW = true;";

                                var initialAddressValue = (up, pre) switch
                                {
                                    (true, true) => "core.R[rn] + 4", // Pre-increment
                                    (false, true) => "(uint)(core.R[rn] - (4 * _storeLoadMultiplePopCount))", // Pre-decrement
                                    (true, false) => "core.R[rn]", // Post-increment
                                    (false, false) => "(uint)(core.R[rn] - (4 * (_storeLoadMultiplePopCount - 1)))", // Post-decrement
                                };
                                initialAddressValue += (load) ? ";" : " - 4;";

                                var finalWritebackValue = (up, writeback) switch
                                {
                                    (_, false) => "",
                                    (true, true) => "_storeLoadMutipleFinalWritebackValue = (uint)(core.R[rn] + (4 * _storeLoadMultiplePopCount));",
                                    (false, true) => "_storeLoadMutipleFinalWritebackValue = (uint)(core.R[rn] - (4 * _storeLoadMultiplePopCount));",
                                };
                                var nextAction = load ? "ldm_registerReadCycle" : "stm_registerWriteCycle";
                                var immediateNextActionStatement = load ? "" : $"{nextAction}(core, instruction);";

                                var loadStateArrayStr = load ? 
                                    "_storeLoadMultipleState[_storeLoadMultiplePopCount] = (uint)r;" : 
                                    "_storeLoadMultipleState[_storeLoadMultiplePopCount] = core.R[r];";

                                var func = $@"
static partial void {funcName}(Core core, uint instruction)
{{
    var rn = (instruction >> 16) & 0b1111;
    var registerList = instruction & 0xFFFF;
    _storeLoadMultiplePopCount = 0;
    _storeLoadMultiplePtr = 0;

    for (var r = 0; r <= 15; r++)
    {{
        if (((registerList >> r) & 0b1) == 0b1)
        {{
            {loadStateArrayStr}
            _storeLoadMultiplePopCount++;
        }}
    }}

    core.nOPC = true;
    core.SEQ = _storeLoadMultiplePopCount > 1;
    core.MAS = BusWidth.Word;
    {nrwStr}
    core.NextExecuteAction = &{nextAction};
    
    {writebackStr}
    {finalWritebackValue}
    core.A = {initialAddressValue}

    {immediateNextActionStatement};
}}

";

                                // This is some gloriously filthy string replacement to refer registers to the banked versions instead of the real versions
                                if (userMode)
                                {
                                    func = func.Replace("core.R[", "core.R_Banked[0][");
                                }

                                fullSource += func;
                            }
                        }
                    }
                }
            }

            fullSource += "}";

            context.AddSource("ArmLdmStmOps.g.cs", fullSource);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // Debugger.Launch(); - Uncomment to allow debugging during build
        }
    }
}
