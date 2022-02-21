using GameboyAdvanced.Core.Cpu;
using GameboyAdvanced.Core.Cpu.Disassembler;
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
    /// <summary>
    /// If R15 is set on a cycle then we need to know so that we know not to 
    /// increment the address bus/R15 during pipeline step.
    /// </summary>
    internal bool ClearedThisCycle;

    internal uint? FetchedOpcode;
    internal uint? FetchedOpcodeAddress;
    internal uint? DecodedOpcode;
    internal uint? DecodedOpcodeAddress;
    internal uint? CurrentInstruction;
    internal uint? CurrentInstructionAddress;

    public Pipeline()
    {
        ClearedThisCycle = false;
        FetchedOpcode = FetchedOpcodeAddress = null;
        DecodedOpcode = DecodedOpcodeAddress = null;
        CurrentInstruction = CurrentInstructionAddress = null;
    }
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
    internal readonly BaseDebugger Debugger;
    internal ulong Cycles;
    internal readonly MemoryBus Bus;
    internal CPSR Cpsr;
    internal readonly CPSR[] Spsr = new CPSR[5];

    // Current registers (irrespective of mode)
    internal readonly uint[] R = new uint[16];

    private readonly uint[] _spBanks = new uint[6];
    private readonly uint[] _lrBanks = new uint[6];
    private readonly uint[] _fiqHiRegs = new uint[5];

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
        Debugger = debugger;
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
                            waitStates = Bus.WriteHalfWord(A & 0xFFFF_FFFE, (ushort)D);
                        }
                        else
                        {
                            (var ret, waitStates) = Bus.ReadHalfWord(A & 0xFFFF_FFFE);
                            D = (D & 0xFFFF_0000) | ret;
                        }

                        break;
                    }
                case BusWidth.Word:
                    if (nRW)
                    {
                        waitStates = Bus.WriteWord(A & 0xFFFF_FFFC, D);
                    }
                    else
                    {
                        (D, waitStates) = Bus.ReadWord(A & 0xFFFF_FFFC);

                        // TODO - I'm not really sure about this, misaligned reads to register rotate the value into the reg but is that all reads? Is it handled on the data bus or somewhere else?
                        var rotate = 8 * (int)(A % 4);
                        D = (D >> rotate) | (D << (32 - rotate));
                    }
                    break;
                default:
                    throw new Exception($"Invalid value for MAS {MAS}");
            }

            WaitStates += waitStates;
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
        R[13] = 0x03007F00; // TODO - Is this correct? It's probably set by bios but roms assume it's here
        ClearPipeline();
        Pipeline.ClearedThisCycle = false; // Despite the pipeline being cleared this is part of RESET so don't skip address increments

        // TODO - What is the initial value of CPSR, do we start in supervisor?
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

        if (!core.Pipeline.CurrentInstruction.HasValue || !core.Pipeline.CurrentInstructionAddress.HasValue)
        {
            // Nothing in the execute unit of the pipeline, skip this cycle
            return;
        }

#if DEBUG
        core.Debugger.Log(core.ToString());
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
        Cycles++;
        if (WaitStates > 0)
        {
            WaitStates--;
            return;
        }

        StepMemoryUnit();

        if (!nOPC && !nMREQ)
        {
            if (Pipeline.DecodedOpcode.HasValue && Pipeline.DecodedOpcodeAddress.HasValue)
            {
                Pipeline.CurrentInstruction = Pipeline.DecodedOpcode.Value;
                Pipeline.CurrentInstructionAddress = Pipeline.DecodedOpcodeAddress.Value;
            }

            if (Pipeline.FetchedOpcode.HasValue && Pipeline.FetchedOpcodeAddress.HasValue)
            {
                Pipeline.DecodedOpcode = Pipeline.FetchedOpcode.Value;
                Pipeline.DecodedOpcodeAddress = Pipeline.FetchedOpcodeAddress.Value;
            }

            // This needs to happen before executing as the contents of D/A may
            // change
            Pipeline.FetchedOpcode = D;
            Pipeline.FetchedOpcodeAddress = A;
        }
        
        // Actually execute anything that's in the right part of the pipeline
        NextExecuteAction(this, Pipeline.CurrentInstruction ?? 0u);

        // If the pipeline was cleared this cycle it was because R15 was just set,
        // in which case we don't want to modify it for OPC
        if (Pipeline.ClearedThisCycle)
        {
            Pipeline.ClearedThisCycle = false;
        }
        else if (A == Pipeline.FetchedOpcodeAddress)
        {
            A += (uint)MAS;
            R[15] = A;
        }
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
        Pipeline.CurrentInstruction = Pipeline.CurrentInstructionAddress = null;
        A = R[15];
        Pipeline.ClearedThisCycle = true;
    }

    /// <summary>
    /// Resets the memory unit to the state required for an opcode fetch.
    /// 
    /// Can be used in two modes, either as a <see cref="NextExecuteAction"/>
    /// during an I cycle or called manually to reset the memory unit.
    /// </summary>
    internal static void ResetMemoryUnitForOpcodeFetch(Core core, uint _)
    {
        core.A = core.R[15];
        core.nOPC = false;
        core.nRW = false;
        core.SEQ = false;
        core.MAS = core.Cpsr.ThumbMode ? BusWidth.HalfWord : BusWidth.Word; // TODO - Could make this faster by making ThumbMode a BusWidth instead of a bool (or some bool -> int shenanigans)
        core.nMREQ = false;
        core.MoveExecutePipelineToNextInstruction();
    }

    internal void SwitchToThumb()
    {
#if DEBUG
        if (!Cpsr.ThumbMode)
        {
            Debugger.FireEvent(DebugEvent.SwitchToThumb, this);
        }
#endif
        Cpsr.ThumbMode = true;
        MAS = BusWidth.HalfWord;
        ClearPipeline();
    }

    internal void SwitchToArm()
    {
#if DEBUG
        if (Cpsr.ThumbMode)
        {
            Debugger.FireEvent(DebugEvent.SwitchToArm, this);
        }
#endif
        Cpsr.ThumbMode = false;
        MAS = BusWidth.Word;
        ClearPipeline();
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
        if (Debugger.CheckBreakOnNextInstruction())
        {
            Debugger.ForceBreakpointNextCycle();
        }

        if (Pipeline.FetchedOpcodeAddress.HasValue && Debugger.BreakOnExecute(Pipeline.FetchedOpcodeAddress.Value))
        {
            Debugger.ForceBreakpointNextCycle();
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CPSR CurrentSpsr() => Spsr[Cpsr.Mode.Index()];

    internal uint GetUserModeRegister(int reg) => (reg, Cpsr.Mode) switch
    {
        (_, CPSRMode.User) => R[reg],
        (13, _) => _spBanks[0],
        (14, _) => _lrBanks[0],
        (15, _) => R[15],
        (_, _) when reg < 8 => R[reg],
        (_, _) when reg < 13 => R[reg], // Would handle FIQ banking here but can't be bothered yet
        _ => throw new Exception("Invalid get user mode register request")
    };

    internal void WriteUserModeRegister(int reg, uint value)
    {
        switch ((reg, Cpsr.Mode))
        {
            case (_, CPSRMode.User):
                R[reg] = value;
                break;
            case (13, _):
                _spBanks[0] = value;
                break;
            case (14, _):
                _lrBanks[0] = value;
                break;
            case (15, _):
#if DEBUG
                if (value == 0)
                {
                    Debugger.FireEvent(Debug.DebugEvent.BranchToZero, this);
                }
#endif
                R[15] = value;
                ClearPipeline();
                break;
            case (_, _) when reg < 8:
                R[reg] = value;
                break;
            case (_, _) when reg < 13:
                R[reg] = value;
                break;
            default:
                throw new Exception("Invalid get user mode register request");
        }
    }

    internal void HandleInterrupt(uint irqVector, uint retAddress, CPSRMode newMode)
    {
        // First save off current bank of registers into mode specific bank
        _spBanks[Cpsr.Mode.Index()] = R[13];
        _lrBanks[Cpsr.Mode.Index()] = R[14];
        if (Cpsr.Mode is CPSRMode.Fiq or CPSRMode.OldFiq)
        {
            throw new NotImplementedException("FIQ not implemented");
        }

        // Then move banked registers into current
        R[13] = _spBanks[newMode.Index()];
        R[14] = retAddress; // Don't care what was banked here as return address will overwrite it
        if (newMode is CPSRMode.Fiq or CPSRMode.OldFiq)
        {
            throw new NotImplementedException("FIQ not implemented");
        }

        // Put the current CPSR into SPSR for the destination mode
        Spsr[newMode.Index()].Set(Cpsr.Get());

        // Set up the interrupt vector and clear the pipeline
        R[15] = irqVector;
        ClearPipeline();
        
        Cpsr.Mode = newMode;
        Cpsr.ThumbMode = false;
        Cpsr.IrqDisable = true;
        Cpsr.FiqDisable = true;
    }

    /// <summary>
    /// This roughly matches the string representation that mGBA uses when
    /// single stepping through the debugger
    public override string ToString()
    {
        string disassembly;
        if (Pipeline.CurrentInstruction.HasValue && Pipeline.CurrentInstructionAddress.HasValue)
        {
            disassembly = Cpsr.ThumbMode
                ? $"{Pipeline.CurrentInstructionAddress:X8}: {(ushort)Pipeline.CurrentInstruction:X4} \t {ThumbDisassembler.Disassemble(this, (ushort)Pipeline.CurrentInstruction)}"
                : $"{Pipeline.CurrentInstructionAddress:X8}: {Pipeline.CurrentInstruction:X8} \t {ArmDisassembler.Disassemble(this, Pipeline.CurrentInstruction.Value)}";

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
Cycle: {Cycles}
{disassembly}";
    }
}
