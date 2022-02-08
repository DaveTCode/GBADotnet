using GameboyAdvanced.Core.Cpu;
using GameboyAdvanced.Core.Debug;
using System.Runtime.CompilerServices;

namespace GameboyAdvanced.Core;

internal enum BusWidth
{
    Byte = 0x1,
    HalfWord = 0x2,
    Word = 0x4,
}

/// <summary>
/// The ARM7TDMI has a 3 stage pipeline where the decode & fetch units can act 
/// in parallel to execution of an instruction.
/// 
/// Here FetchedOpcode is set to the value of the data bus (D) on the same 
/// cycle it is loaded.
/// DecodedOpcode is set to the value of FetchedOpcode by the decode function
/// just before D is loaded into FetchedOpcode.
/// 
/// The execute unit uses the value in DecodedOpcode to execute the 
/// instruction.
/// </summary>
internal struct Pipeline
{
    internal uint? FetchedOpcode;
    internal uint? FetchedOpcodeAddress;
    internal uint? DecodedOpcode;
    internal uint? DecodedOpcodeAddress;
    internal uint? CurrentInstruction;
    internal uint? CurrentInstructionAddress;
}

/// <summary>
/// This class implements the parts of the ARM7TDMI core that are required for 
/// accurate GBA emulation.
/// 
/// Notably there are several key signals which we do not need to emulate 
/// because the GBA MMU doesn't use them (e.g. TBIT, APE/ALE)
/// 
/// The CPU implemented here is little endian only, always runs in pipelined 
/// mode and steps in whole mCLK increments (so rising/falling edge differences
/// are not guaranteed to be well implemented)
/// </summary>
public unsafe class Core
{
    private readonly BaseDebugger _debugger;
    private ulong _cycles;
    internal readonly MemoryBus Bus;
    internal CPSR Cpsr;
    internal readonly CPSR[] Spsr = new CPSR[5];

    // Current registers (irrespective of mode)
    internal readonly uint[] R = new uint[16];

    // Banked versions of registers, note that in most cases all banks will
    // have the same value. This invariant is handled during mode switch.
    internal readonly uint[][] R_Banked = new uint[5][]
    {
        new uint[16],
        new uint[16],
        new uint[16],
        new uint[16],
        new uint[16]
    };

    /// <summary>
    /// The 32 bit address bus value, set the cycle before it is used
    /// </summary>
    internal uint A;

    /// <summary>
    /// The 32 bit data bus value (DIN/DOUT not split here), set by 
    /// the memory fetch unit at the start of a cycle.
    /// </summary>
    internal uint D;

    /// <summary>
    /// Represents MAS[1:0] and is set the cycle before it is used to the
    /// amount of data that will be requested during a memory fetch
    /// </summary>
    internal BusWidth MAS;

    /// <summary>
    /// nMREQ is set low when a memory request will occur on a cycle and high
    /// when it will not (e.g. an I cycle)
    /// </summary>
    internal bool nMREQ;

    /// <summary>
    /// SEQ is set high when a memory request will be sequential and low when
    /// it will not (e.g. during a branch operation when the branch address is 
    /// first fetched)
    /// </summary>
    internal bool SEQ;

    /// <summary>
    /// nOPC (not Opcode Fetch) is set low when a memory fetch is for an opcode
    /// and set high when it is not. This is particularly relevant to e.g. bios
    /// memory requests which only allow reads when nOPC is low.
    /// </summary>
    internal bool nOPC;

    /// <summary>
    /// nRW is set high when the memory unit should perform a write of D to [A]
    /// on the next cycle and set low when the memory unit should perform a 
    /// read of [A] into D on the next cycle.
    /// </summary>
    internal bool nRW;

    /// <summary>
    /// Rather than emulating the nWAIT signal as a flag we instead set 
    /// WaitStates to the number of cycles during which the CPU will perform
    /// no actions.
    /// 
    /// This is set by the memory unit when reading/writing to the bus if the
    /// bus returns that the component requires wait states (e.g. GamePak)
    /// </summary>
    internal int WaitStates;

    /// <summary>
    /// Contains the current state of the 3 stage pipeline
    /// </summary>
    internal Pipeline Pipeline;

    /// <summary>
    /// Whilst the memory and decode units will always execute the same logic
    /// on each cycle, the execute unit can sometimes take multiple cycles to
    /// complete an action (e.g. MUL). This function pointer is set at the end
    /// of each execute action to the next in sequence, normally that is 
    /// <see cref="ExecuteFirstInstructionCycle"/> but not always.
    /// </summary>
    internal delegate*<Core, uint, void> NextExecuteAction;

    /// <summary>
    /// Used by the debugger to determine if the instruction that's currently 
    /// executing is the "first" so we can break before it happens.
    /// </summary>
    internal bool IsFirstInstructionCycle { private set; get; }

    internal Core(MemoryBus bus, uint startVector, BaseDebugger debugger)
    {
        Bus = bus;
        _debugger = debugger;
        Reset(startVector);
    }

    /// <summary>
    /// This function steps the memory unit and is called on every mCLK cycle.
    /// 
    /// It will internally only act if nMREQ is pulled low and will either 
    /// write D into [A] if nRW is high or read [A] into D if nRW is low.
    /// </summary>
    internal void StepMemoryUnit()
    {
        if (!nMREQ)
        {
            int waitStates;
            switch (MAS)
            {
                case BusWidth.Byte:
                    {
                        if (nRW)
                        {
                            waitStates = Bus.WriteByte(A, (byte)D);
                        }
                        else
                        {
                            (var ret, waitStates) = Bus.ReadByte(A);
                            D = (D & 0xFFFF_FF00) | ret;
                        }
                        break;
                    }
                case BusWidth.HalfWord:
                    {
                        if (nRW)
                        {
                            waitStates = Bus.WriteHalfWord(A, (ushort)D);
                        }
                        else
                        {
                            (var ret, waitStates) = Bus.ReadHalfWord(A);
                            D = (D & 0xFFFF_0000) | ret;
                        }

                        break;
                    }
                case BusWidth.Word:
                    if (nRW)
                    {
                        waitStates = Bus.WriteWord(A, D);
                    }
                    else
                    {
                        (D, waitStates) = Bus.ReadWord(A);
                    }
                    break;
                default:
                    throw new Exception($"Invalid value for MAS {MAS}");
            }

            WaitStates += waitStates;
        }
    }

    /// <summary>
    /// This function is responsible for stepping the pipeline.
    /// 
    /// It is critical that it occurs _after_ stepping the memory unit as this
    /// function is responsible for incrementing R[15]/PC and announcing it on
    /// the address bus (A) for the next fetch cycle.
    /// </summary>
    internal void StepPipeline()
    {
        if (Pipeline.FetchedOpcode.HasValue && Pipeline.FetchedOpcodeAddress.HasValue)
        {
            Pipeline.DecodedOpcode = Pipeline.FetchedOpcode.Value;
            Pipeline.DecodedOpcodeAddress = Pipeline.FetchedOpcodeAddress.Value;
        }

        if (!nOPC && !nMREQ)
        {
            Pipeline.FetchedOpcode = D;
            Pipeline.FetchedOpcodeAddress = A;
            A += (uint)MAS;
            R[15] = A;
        }
    }

    internal void Reset(uint startVector)
    {
        A = startVector;
        D = 0x0;
        MAS = BusWidth.Word;
        nMREQ = false;
        SEQ = false;
        nOPC = false;
        nRW = false;
        WaitStates = 0;
        Pipeline = new Pipeline
        {
            DecodedOpcode = null,
            FetchedOpcode = null
        };

        Array.Clear(R, 0, R.Length);
        R[15] = startVector;
        R_Banked[0][15] = startVector;
        R[13] = 0x03007F00; // TODO - Is this correct? It's probably set by bios but roms assume it's here
        R_Banked[0][13] = 0x03007F00;
        ClearPipeline();

        // TODO - What is the initial value of CPSR
        Cpsr.FiqDisable = true;
        Cpsr.IrqDisable = true;
        Cpsr.Mode = CPSRMode.Supervisor;

        MoveExecutePipelineToNextInstruction();
    }


    /// <summary>
    /// The first cycle of every instruction is the same, check the condition
    /// and either skip or perform the initial part of the operation.
    /// 
    /// This function (and those it calls) are responsible for updating the 
    /// state on the CPU including setting the NextExecuteAction function 
    /// pointer.
    /// </summary>
    internal static void ExecuteFirstInstructionCycle(Core core, uint _)
    {
#if DEBUG
        core.IsFirstInstructionCycle = false;
#endif

        if (core.Pipeline.DecodedOpcode.HasValue && core.Pipeline.DecodedOpcodeAddress.HasValue)
        {
            core.Pipeline.CurrentInstruction = core.Pipeline.DecodedOpcode.Value;
            core.Pipeline.CurrentInstructionAddress = core.Pipeline.DecodedOpcodeAddress.Value;
        }
        else
        {
            // Nothing in the execute unit of the pipeline, skip this cycle
            return;
        }

#if DEBUG
        core._debugger.Log(core.ToString());
#endif

        var instruction = core.Pipeline.CurrentInstruction.Value;

        if (core.Cpsr.ThumbMode)
        {
            var thumbInstruction = (ushort)instruction;
            var instructionIndex = thumbInstruction >> 8;
            Thumb.InstructionMap[instructionIndex](core, thumbInstruction);
        }
        else
        {
            var cond = instruction >> 28;

            var condAcc = cond switch
            {
                0 => core.Cpsr.ZeroFlag,
                1 => !core.Cpsr.ZeroFlag,
                2 => core.Cpsr.CarryFlag,
                3 => !core.Cpsr.CarryFlag,
                4 => core.Cpsr.SignFlag,
                5 => !core.Cpsr.SignFlag,
                6 => core.Cpsr.OverflowFlag,
                7 => !core.Cpsr.OverflowFlag,
                8 => core.Cpsr.CarryFlag && !core.Cpsr.ZeroFlag,
                9 => !core.Cpsr.CarryFlag || core.Cpsr.ZeroFlag,
                0xA => core.Cpsr.SignFlag == core.Cpsr.OverflowFlag,
                0xB => core.Cpsr.SignFlag != core.Cpsr.OverflowFlag,
                0xC => !core.Cpsr.ZeroFlag && (core.Cpsr.SignFlag == core.Cpsr.OverflowFlag),
                0xD => core.Cpsr.ZeroFlag || (core.Cpsr.SignFlag != core.Cpsr.OverflowFlag),
                0xE => true,
                0xF => false, // TODO - "never (ARMv1,v2 only) (Reserved ARMv3 and up)" - ok but what happens if it _is_ set
                _ => throw new Exception("Invalid state"),
            };

            if (condAcc)
            {
                var functionTablePtr = ((instruction >> 16) & 0xFF0) | ((instruction >> 4) & 0x00F);

                Arm.InstructionMap[functionTablePtr](core, instruction);
            }
        }
    }

    /// <summary>
    /// Execute a single mCLK rising/falling edge cycle for the entire CPU unit
    /// </summary>
    /// 
    /// <remarks>
    /// Ordering of actions here is vitally important, stepping the memory unit 
    /// will retrieve whatever is in [A] and place it in D, since A is 
    /// announced in the previous cycle that means that the pipeline and 
    /// execute units expect D to be filled before they run.
    /// </remarks>
    internal void Clock()
    {
        _cycles++;
        if (WaitStates > 0)
        {
            WaitStates--;
            return;
        }

        StepMemoryUnit();
        StepPipeline();
        NextExecuteAction(this, Pipeline.CurrentInstruction ?? 0u);
    }

    /// <summary>
    /// The pipeline is cleared on any operation which affects R[15] whether 
    /// directly (e.g. branch) or indirectly by using R[15] as the destination
    /// register.
    /// 
    /// Clearing the pipeline also immediately forces it to be refilled with 
    /// two fetches (one of which may be at new PC so PC must be updated 
    /// before calling).
    /// </summary>
    internal void ClearPipeline()
    {
        Pipeline.FetchedOpcode = Pipeline.FetchedOpcodeAddress = null;
        Pipeline.DecodedOpcode = Pipeline.DecodedOpcodeAddress = null;
    }

    internal static void ResetMemoryUnitForArmOpcodeFetch(Core core, uint _) => ResetMemoryUnitForOpcodeFetch(core, BusWidth.Word);

    internal static void ResetMemoryUnitForThumbOpcodeFetch(Core core, uint _) => ResetMemoryUnitForOpcodeFetch(core, BusWidth.HalfWord);

    /// <summary>
    /// Resets the memory unit to the state required for an opcode fetch.
    /// 
    /// Can be used in two modes, either as a <see cref="NextExecuteAction"/>
    /// during an I cycle or called manually to reset the memory unit.
    /// </summary>
    private static void ResetMemoryUnitForOpcodeFetch(Core core, BusWidth busWidth)
    {
        core.A = core.R[15];
        core.nOPC = false;
        core.nRW = false;
        core.SEQ = false;
        core.MAS = busWidth;
        core.nMREQ = false;
        core.MoveExecutePipelineToNextInstruction();
    }

    internal void SwitchToThumb()
    {
        Cpsr.ThumbMode = true;
        MAS = BusWidth.HalfWord;
        ClearPipeline();

        // TODO - Do we need to do anything else here? Feels a bit bare!
    }

    internal void SwitchToArm()
    {
        Cpsr.ThumbMode = false;
        MAS = BusWidth.Word;
        ClearPipeline();

        // TODO - Do we need to do anything else here? Feels a bit bare!
    }

    /// <summary>
    /// When an instruction has finished executing it calls this function to 
    /// ensure that the next execute step is a "first cycle".
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void MoveExecutePipelineToNextInstruction()
    {
        NextExecuteAction = &ExecuteFirstInstructionCycle;
#if DEBUG
        if (_debugger.CheckBreakOnNextInstruction())
        {
            _debugger.ForceBreakpointNextCycle();
        }

        if (Pipeline.FetchedOpcodeAddress.HasValue && _debugger.BreakOnExecute(Pipeline.FetchedOpcodeAddress.Value))
        {
            _debugger.ForceBreakpointNextCycle();
        }
#endif
    }

    internal CPSR CurrentSpsr() => Cpsr.Mode switch
    {
        CPSRMode.OldUser => Spsr[0],
        CPSRMode.OldFiq => Spsr[1],
        CPSRMode.OldIrq => Spsr[4],
        CPSRMode.OldSupervisor => Spsr[2],
        CPSRMode.User => Spsr[0],
        CPSRMode.Fiq => Spsr[1],
        CPSRMode.Irq => Spsr[4],
        CPSRMode.Supervisor => Spsr[2],
        CPSRMode.Abort => Spsr[3],
        CPSRMode.Undefined => Spsr[5],
        CPSRMode.System => Spsr[0],
        _ => throw new Exception($"Invalid cpsr mode {Cpsr.Mode}"),
    };

    /// <summary>
    /// This roughly matches the string representation that mGBA uses when
    /// single stepping through the debugger
    public override string ToString()
    {
        string disassembly;
        if (Pipeline.DecodedOpcode.HasValue && Pipeline.DecodedOpcodeAddress.HasValue)
        {
            disassembly = Cpsr.ThumbMode
                ? $"{Pipeline.DecodedOpcodeAddress:X8}: {(ushort)Pipeline.DecodedOpcode:X4} \t {Disassembler.DisassembleThumbInstruction(this, (ushort)Pipeline.DecodedOpcode)}"
                : $"{Pipeline.DecodedOpcodeAddress:X8}: {Pipeline.DecodedOpcode:X8} \t {Disassembler.DisassembleArmInstruction(this, Pipeline.DecodedOpcode.Value)}";

        }
        else
        {
            disassembly = "Pipeline not refilled";
        }

        return $@"
 r0:{R[0]:X8}   r1:{R[1]:X8}   r2:{R[2]:X8}   r3:{R[3]:X8}
 r4:{R[4]:X8}   r5:{R[5]:X8}   r6:{R[6]:X8}   r7:{R[7]:X8}
 r8:{R[8]:X8}   r9:{R[9]:X8}  r10:{R[10]:X8}  r11:{R[11]:X8} 
r12:{R[12]:X8}  r13:{R[13]:X8}  r14:{R[14]:X8}  r15:{R[15]:X8}
cpsr: {Cpsr}
Cycle: {_cycles}
{disassembly}";
    }
}
