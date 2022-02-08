using System.Runtime.CompilerServices;
using static GameboyAdvanced.Core.Cpu.ALU;

namespace GameboyAdvanced.Core.Cpu;

/// <summary>
/// This class contains utility methods for the thumb instruction set
/// </summary>
internal unsafe static class Thumb
{
    internal readonly static delegate*<Core, ushort, void>[] InstructionMap =
    {
        // LSL{S} Rd,Rs,#Offset - 0x00 - 0x07
        &LSL_Shift_Reg, &LSL_Shift_Reg, &LSL_Shift_Reg, &LSL_Shift_Reg, &LSL_Shift_Reg, &LSL_Shift_Reg, &LSL_Shift_Reg, &LSL_Shift_Reg,
        // LSR{S} Rd,Rs,#Offset - 0x08 - 0x0F
        &LSR_Shift_Reg, &LSR_Shift_Reg, &LSR_Shift_Reg, &LSR_Shift_Reg, &LSR_Shift_Reg, &LSR_Shift_Reg, &LSR_Shift_Reg, &LSR_Shift_Reg,
        // LSR{S} Rd,Rs,#Offset - 0x10 - 0x17
        &ASR_Shift_Reg, &ASR_Shift_Reg, &ASR_Shift_Reg, &ASR_Shift_Reg, &ASR_Shift_Reg, &ASR_Shift_Reg, &ASR_Shift_Reg, &ASR_Shift_Reg,
        // ADD{S} Rd,Rs,Rn      - 0x18 - 0x19
        &ADD_Reg, &ADD_Reg, 
        // SUB{S} Rd,Rs,Rn      - 0x1A - 0x1B
        &SUB_Reg, &SUB_Reg, 
        // ADD{S} Rd,Rs,#nn     - 0x1C - 0x1D
        &ADD_Imm, &ADD_Imm, 
        // SUB{S} Rd,Rs,#nn     - 0x1E - 0x1F
        &SUB_Imm, &SUB_Imm,
        // MOV{S} Rd,#nn        - 0x20 - 0x27
        &MOV, &MOV, &MOV, &MOV, &MOV, &MOV, &MOV, &MOV,
        // CMP{S} Rd,#nn        - 0x28 - 0x2F
        &CMP, &CMP, &CMP, &CMP, &CMP, &CMP, &CMP, &CMP,
        // ADD{S} Rd,#nn        - 0x30 - 0x37
        &ADD_Imm_B, &ADD_Imm_B, &ADD_Imm_B, &ADD_Imm_B, &ADD_Imm_B, &ADD_Imm_B, &ADD_Imm_B, &ADD_Imm_B,
        // SUB{S} Rd,#nn        - 0x37 - 0x3F
        &SUB_Imm_B, &SUB_Imm_B, &SUB_Imm_B, &SUB_Imm_B, &SUB_Imm_B, &SUB_Imm_B, &SUB_Imm_B, &SUB_Imm_B,
        // ALU Operations       - 0x40 - 0x43
        &ALU, &ALU, &ALU, &ALU,
        // R8-15 operations and BX/BLX - 0x44 - 0x47
        &HiReg_Or_BX, &HiReg_Or_BX, &HiReg_Or_BX, &HiReg_Or_BX,
        // LDR Rd,[PC,#nn]      - 0x48 - 0x4F
        &LDR_PC_Offset, &LDR_PC_Offset, &LDR_PC_Offset, &LDR_PC_Offset, &LDR_PC_Offset, &LDR_PC_Offset, &LDR_PC_Offset, &LDR_PC_Offset,
        // STR  Rd,[Rb,Ro]      - 0x50 - 0x53
        &STR_Reg_Offset, &STR_Reg_Offset, &STR_Reg_Offset, &STR_Reg_Offset,
        // STRB Rd,[Rb,Ro]      - 0x54 - 0x57
        &STRB_Reg_Offset, &STRB_Reg_Offset, &STRB_Reg_Offset, &STRB_Reg_Offset,
        // LDR  Rd,[Rb,Ro]      - 0x58 - 0x5B
        &LDR_Reg_Offset, &LDR_Reg_Offset, &LDR_Reg_Offset, &LDR_Reg_Offset,
        // LDRB Rd,[Rb,Ro]      - 0x5C - 0x5F
        &LDRB_Reg_Offset, &LDRB_Reg_Offset, &LDRB_Reg_Offset, &LDRB_Reg_Offset,
        // STRH Rd,[Rb,Ro]
        &STRH_Reg_Offset, &STRH_Reg_Offset, &STRH_Reg_Offset, &STRH_Reg_Offset,
        // LDSB Rd,[Rb,Ro]
        &LDSB_Reg_Offset, &LDSB_Reg_Offset, &LDSB_Reg_Offset, &LDSB_Reg_Offset,
        // LDRH Rd,[Rb,Ro]
        &LDRH_Reg_Offset, &LDRH_Reg_Offset, &LDRH_Reg_Offset, &LDRH_Reg_Offset,
        // LDSH Rd,[Rb,Ro]
        &LDSH_Reg_Offset, &LDSH_Reg_Offset, &LDSH_Reg_Offset, &LDSH_Reg_Offset,
        // STR  Rd,[Rb,#nn]
        &STR_Imm_Offset, &STR_Imm_Offset, &STR_Imm_Offset, &STR_Imm_Offset, &STR_Imm_Offset, &STR_Imm_Offset, &STR_Imm_Offset, &STR_Imm_Offset,
        // LDR  Rd,[Rb,#nn]
        &LDR_Imm_Offset, &LDR_Imm_Offset, &LDR_Imm_Offset, &LDR_Imm_Offset, &LDR_Imm_Offset, &LDR_Imm_Offset, &LDR_Imm_Offset, &LDR_Imm_Offset,
        // STRB Rd,[Rb,#nn]
        &STRB_Imm_Offset, &STRB_Imm_Offset, &STRB_Imm_Offset, &STRB_Imm_Offset, &STRB_Imm_Offset, &STRB_Imm_Offset, &STRB_Imm_Offset, &STRB_Imm_Offset,
        // LDRB Rd,[Rb,#nn]
        &LDRB_Imm_Offset, &LDRB_Imm_Offset, &LDRB_Imm_Offset, &LDRB_Imm_Offset, &LDRB_Imm_Offset, &LDRB_Imm_Offset, &LDRB_Imm_Offset, &LDRB_Imm_Offset,
        // STRH Rd,[Rb,#nn]
        &STRH, &STRH, &STRH, &STRH, &STRH, &STRH, &STRH, &STRH,
        // LDRH Rd,[Rb,#nn]
        &LDRH, &LDRH, &LDRH, &LDRH, &LDRH, &LDRH, &LDRH, &LDRH,
        // STR  Rd,[SP,#nn]
        &STR_SP_Rel, &STR_SP_Rel, &STR_SP_Rel, &STR_SP_Rel, &STR_SP_Rel, &STR_SP_Rel, &STR_SP_Rel, &STR_SP_Rel,
        // LDR  Rd,[SP,#nn]
        &LDR_SP_Rel, &LDR_SP_Rel, &LDR_SP_Rel, &LDR_SP_Rel, &LDR_SP_Rel, &LDR_SP_Rel, &LDR_SP_Rel, &LDR_SP_Rel,
        // ADD  Rd,PC,#nn
        &Get_Rel_PC, &Get_Rel_PC, &Get_Rel_PC, &Get_Rel_PC, &Get_Rel_PC, &Get_Rel_PC, &Get_Rel_PC, &Get_Rel_PC,
        // ADD  Rd,SP,#nn
        &Get_Rel_SP, &Get_Rel_SP, &Get_Rel_SP, &Get_Rel_SP, &Get_Rel_SP, &Get_Rel_SP, &Get_Rel_SP, &Get_Rel_SP,
        // ADD  SP,#nn, ADD  SP,#-nn
        &ADD_Offset_SP,
        // PUSH {Rlist}{LR}
        &PUSH, &PUSH, &PUSH, &PUSH, &PUSH, &PUSH, &PUSH, &PUSH,
        // POP  {Rlist}{LR}
        &POP, &POP, &POP, &POP, &POP, &POP, &POP, &POP,
        // STMIA Rb!,{Rlist}
        &STMIA, &STMIA, &STMIA, &STMIA, &STMIA, &STMIA, &STMIA, &STMIA,
        // LDMIA Rb!,{Rlist}
        &LDMIA, &LDMIA, &LDMIA, &LDMIA, &LDMIA, &LDMIA, &LDMIA, &LDMIA,
    };

    public static void LSL_Shift_Reg(Core core, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        core.R[rd] = LSL(core.R[rs], (byte)offset, ref core.Cpsr);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void LSR_Shift_Reg(Core core, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        core.R[rd] = LSR(core.R[rs], (byte)offset, ref core.Cpsr);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void ASR_Shift_Reg(Core core, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        core.R[rd] = ASR(core.R[rs], (byte)offset, ref core.Cpsr);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void ADD_Reg(Core core, ushort instruction)
    {
        var rn = (instruction >> 6) & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        core.R[rd] = ADD(core.R[rs], core.R[rn], ref core.Cpsr);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void ADD_Imm(Core core, ushort instruction)
    {
        var nn = (instruction >> 6) & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        core.R[rd] = ADD(core.R[rs], (uint)nn, ref core.Cpsr);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void SUB_Reg(Core core, ushort instruction)
    {
        var rn = (instruction >> 6) & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        core.R[rd] = SUB(core.R[rs], core.R[rn], ref core.Cpsr);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void SUB_Imm(Core core, ushort instruction)
    {
        var nn = (instruction >> 6) & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        core.R[rd] = SUB(core.R[rs], (uint)nn, ref core.Cpsr);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void MOV(Core core, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var nn = instruction & 0xFF;

        core.R[rd] = (uint)nn;
        SetZeroSignFlags(ref core.Cpsr, core.R[rd]);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void CMP(Core core, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var nn = instruction & 0xFF;

        _ = SUB(core.R[rd], (uint)nn, ref core.Cpsr);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void ADD_Imm_B(Core core, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var nn = instruction & 0xFF;

        core.R[rd] = ADD(core.R[rd], (uint)nn, ref core.Cpsr);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void SUB_Imm_B(Core core, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var nn = instruction & 0xFF;

        core.R[rd] = SUB(core.R[rd], (uint)nn, ref core.Cpsr);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void ALU(Core core, ushort instruction)
    {
        var opcode = (instruction >> 6) & 0b1111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        core.MoveExecutePipelineToNextInstruction();

        switch (opcode)
        {
            case 0x0: // AND
                core.R[rd] = core.R[rd] & core.R[rs];
                SetZeroSignFlags(ref core.Cpsr, core.R[rd]);
                break;
            case 0x1: // EOR
                core.R[rd] = core.R[rd] ^ core.R[rs];
                SetZeroSignFlags(ref core.Cpsr, core.R[rd]);
                break;
            case 0x2: // LSL
                core.R[rd] = LSL(core.R[rd], (byte)core.R[rs], ref core.Cpsr);
                core.WaitStates++; // Extra I cycle
                break;
            case 0x3: // LSR
                core.R[rd] = LSR(core.R[rd], (byte)core.R[rs], ref core.Cpsr);
                core.WaitStates++; // Extra I cycle
                break;
            case 0x4: // ASR
                core.R[rd] = ASR(core.R[rd], (byte)core.R[rs], ref core.Cpsr);
                core.WaitStates++; // Extra I cycle
                break;
            case 0x5: // ADC
                core.R[rd] = ADC(core.R[rd], core.R[rs], ref core.Cpsr);
                break;
            case 0x6: // SBC
                core.R[rd] = SBC(core.R[rd], core.R[rs], ref core.Cpsr);
                break;
            case 0x7: // ROR
                core.R[rd] = ROR(core.R[rd], (byte)core.R[rs], ref core.Cpsr);
                core.WaitStates++; // Extra I cycle
                break;
            case 0x8: // TST
                var result = core.R[rd] & core.R[rs];
                SetZeroSignFlags(ref core.Cpsr, result);
                break;
            case 0x9: // NEG
                core.R[rd] = SUB(0, core.R[rs], ref core.Cpsr);
                break;
            case 0xA: // CMP
                _ = SUB(0, core.R[rs], ref core.Cpsr);
                break;
            case 0xB: // CMN
                _ = ADD(0, core.R[rs], ref core.Cpsr);
                break;
            case 0xC: // ORR
                core.R[rd] = core.R[rd] | core.R[rs];
                SetZeroSignFlags(ref core.Cpsr, core.R[rd]);
                break;
            case 0xD: // MUL
                throw new NotImplementedException("MUL not implemented yet");
            case 0xE: // BIC
                core.R[rd] = core.R[rd] & (~core.R[rs]);
                SetZeroSignFlags(ref core.Cpsr, core.R[rd]);
                break;
            case 0xF: // MVN
                core.R[rd] = ~core.R[rs];
                SetZeroSignFlags(ref core.Cpsr, core.R[rd]);
                break;
            default:
                throw new NotImplementedException($"Thumb ALU opcode {opcode:X2} not implemented");
        }
    }

    public static void HiReg_Or_BX(Core core, ushort instruction)
    {
        var opcode = (instruction >> 8) & 0b11;
        var msbd = (instruction >> 7) & 0b1;
        var msbs = (instruction >> 6) & 0b1;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        var fullRd = rd | (msbd << 3);
        var fullRs = rs | (msbs << 3);

        var operand = fullRs switch
        {
            15 => (core.R[15] + 4) & 0xFFFF_FFFE,
            _ => core.R[fullRs]
        };

        switch (opcode)
        {
            // TODO - "Restrictions: For ADD/CMP/MOV, MSBs and/or MSBd must be set, ie. it is not allowed that both are cleared." - ok, but what happens if they're not?
            // ADD
            case 0b00:
                core.R[fullRd] = core.R[fullRd] + operand;
                if (fullRd == 15)
                {
                    core.ClearPipeline();
                }
                core.MoveExecutePipelineToNextInstruction();
                break;
            // CMP
            case 0b01:
                _ = SUB(core.R[fullRd], operand, ref core.Cpsr);
                core.MoveExecutePipelineToNextInstruction();
                break;
            // MOV/NOP
            case 0b10:
                core.R[fullRd] = operand;
                if (fullRd == 15)
                {
                    core.ClearPipeline();
                }
                core.MoveExecutePipelineToNextInstruction();
                break;
            // BX/BLX
            case 0b11:
                if ((core.R[fullRs] & 0b1) != 1)
                {
                    core.SwitchToArm();
                }
                
                core.R[15] = core.Cpsr.ThumbMode ? core.R[fullRs] & 0xFFFF_FFFE : core.R[fullRs] & 0xFFFF_FFFC;
                core.A = core.R[15];

                // TODO - http://www.problemkaputt.de/gbatek.htm#thumbinstructionsummary suggests that CLX happens if MSBd is set, other docs don't
                core.MoveExecutePipelineToNextInstruction();
                break;
            default:
                throw new Exception("Invalid");
        }
    }

    #region Load Register

    private static int _ldrReg;
    private static delegate*<uint, uint> _ldrCastFunc;
    
    private static uint LDRW(uint dataBus) => dataBus;
    private static uint LDRHW(uint dataBus) => (ushort)dataBus;
    private static uint LDRSHW(uint dataBus)
    {
        // "and set bits 16 - 31 of Rd to bit 15"
        // TODO - Is there a more efficient way to sign extend in C# which doesn't branch?
        var bit15 = (dataBus >> 15) & 0b1;
        return bit15 == 1 
            ? 0xFFFF_0000 | (ushort)(short)dataBus 
            : (ushort)(short)dataBus;
    }

    private static uint LDRB(uint dataBus) => (byte)dataBus;
    private static uint LDRSB(uint dataBus)
    {
        // "and set bits 8 - 31 of Rd to bit 7"
        // TODO - Is there a more efficient way to sign extend in C# which doesn't branch?
        var bit7 = (dataBus >> 7) & 0b1;
        return bit7 == 1
            ? 0xFFFF_FF00 | (ushort)(sbyte)dataBus
            : (ushort)(sbyte)dataBus;
    }

    /// <summary>
    /// LDR takes 3 cycles (1N + 1S + 1I).
    /// 
    /// The first cycle (e.g. LDR_PC_Offset) calculates the address and puts it 
    /// on the address bus.
    /// 
    /// The second cycle (this one) is a noop from the point of view of the executing 
    /// pipeline (but is when the memory unit will retrieve A into D)
    /// 
    /// The third cycle is <see cref="LDRCycle3(Core, uint)"/>
    /// </summary>
    internal static void LDRCycle2(Core core, uint instruction)
    {
        // After an LDR the address bus (A) shows current PC + 2n and it's set up
        // for opcode fetch but nMREQ is driven high for one internal cycle
        core.A = core.R[15];
        core.nOPC = false;
        core.nRW = false;
        core.SEQ = false;
        core.nMREQ = true;
        core.MAS = BusWidth.HalfWord;

        // "This third cycle can normally be merged with 
        // the next prefetch cycle to form one memory N - cycle"
        // -
        // TODO, what does that mean here, does nMREQ actually go low in most
        // cases? How to know?
        core.NextExecuteAction = &LDRCycle3;
    }

    /// <summary>
    /// LDR takes 3 cycles (1N + 1S + 1I). <see cref="LDRCycle2(Core, uint)"/> 
    /// for details of cycle 1 & 2.
    /// 
    /// On cycle 3 the data bus value is written back into the destination 
    /// register and nMREQ is driven low so that the next cycle will cause
    /// an opcode fetch.
    /// </summary>
    internal static void LDRCycle3(Core core, uint instruction)
    {
        core.R[_ldrReg] = _ldrCastFunc(core.D); // TODO - Do I need to take into account bus width here or will D already be truncated?
        core.nMREQ = false;

        core.MoveExecutePipelineToNextInstruction();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void LDRCommon(Core core, uint address, BusWidth busWidth, int destinationReg, delegate*<uint, uint> castFunc)
    {
        _ldrCastFunc = castFunc;
        _ldrReg = destinationReg;
        core.A = address;
        core.MAS = busWidth;
        core.nRW = false;
        core.nOPC = true;
        core.SEQ = false;
        core.NextExecuteAction = &LDRCycle2;
    }

    public static void LDR_PC_Offset(Core core, ushort instruction)
    {
        var word = instruction & 0xFF;
        LDRCommon(core, (uint)((core.R[15] & ~3) + (word << 2)), BusWidth.Word, (instruction >> 8) & 0b111, &LDRW);
    }

    public static void LDR_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        LDRCommon(core, core.R[rb] + core.R[ro], BusWidth.Word, instruction & 0b111, &LDRW);
    }

    public static void LDRB_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        LDRCommon(core, core.R[rb] + core.R[ro], BusWidth.Byte, instruction & 0b111, &LDRB);
    }

    public static void LDSB_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        LDRCommon(core, core.R[rb] + core.R[ro], BusWidth.Byte, instruction & 0b111, &LDRSB);
    }

    public static void LDRH_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        LDRCommon(core, core.R[rb] + core.R[ro], BusWidth.HalfWord, instruction & 0b111, &LDRHW);
    }

    public static void LDSH_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        LDRCommon(core, core.R[rb] + core.R[ro], BusWidth.HalfWord, instruction & 0b111, &LDRSHW);
    }

    public static void LDRB_Imm_Offset(Core core, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rb = (instruction >> 3) & 0b111;
        LDRCommon(core, (uint)(core.R[rb] + offset), BusWidth.Byte, instruction & 0b111, &LDRB);
    }

    public static void LDRH(Core core, ushort instruction)
    {
        var offset = ((instruction >> 6) & 0b1_1111) << 1;
        var rb = (instruction >> 3) & 0b111;
        LDRCommon(core, (uint)(core.R[rb] + offset), BusWidth.HalfWord, instruction & 0b111, &LDRHW);
    }

    public static void LDR_SP_Rel(Core core, ushort instruction)
    {
        var offset = (instruction & 0xFF) << 2;
        LDRCommon(core, (uint)(core.R[13] + offset), BusWidth.Word, (instruction >> 8) & 0b111, &LDRW);
    }

    public static void LDR_Imm_Offset(Core core, ushort instruction)
    {
        var offset = ((instruction >> 6) & 0b1_1111) << 2;
        var rb = (instruction >> 3) & 0b111;
        LDRCommon(core, (uint)(core.R[rb] + offset), BusWidth.Word, instruction & 0b111, &LDRW);
    }

    #endregion

    #region Store Register

    private static void STRCommon(Core core, uint address, uint data, BusWidth busWidth)
    {
        core.A = address;
        core.D = data;
        core.MAS = busWidth;
        core.nRW = true;
        core.nOPC = true;
        core.SEQ = false;
        core.NextExecuteAction = &Core.ResetMemoryUnitForThumbOpcodeFetch;
    }

    public static void STR_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        STRCommon(core, core.R[rb] + core.R[ro], core.R[rd], BusWidth.Word);
    }

    public static void STRB_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        STRCommon(core, core.R[rb] + core.R[ro], (byte)core.R[rd], BusWidth.Byte);
    }

    public static void STRH_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        STRCommon(core, core.R[rb] + core.R[ro], (ushort)core.R[rd], BusWidth.HalfWord);
    }

    public static void STR_Imm_Offset(Core core, ushort instruction)
    {
        // For word accesses (B = 0), the value specified by #Imm is a full 7-bit address, but must
        // be word-aligned(ie with bits 1:0 set to 0), since the assembler places #Imm >> 2 in
        // the Offset5 field
        var offset = ((instruction >> 6) & 0b1_1111) << 2;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        STRCommon(core, (uint)(core.R[rb] + offset), core.R[rd], BusWidth.Word);
    }

    public static void STRB_Imm_Offset(Core core, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        STRCommon(core, (uint)(core.R[rb] + offset), (byte)core.R[rd], BusWidth.Byte);
    }

    public static void STRH(Core core, ushort instruction)
    {
        // #Imm is a full 6-bit address but must be halfword-aligned (ie with bit 0 set to 0) since
        // the assembler places #Imm >> 1 in the Offset5 field
        var offset = ((instruction >> 6) & 0b1_1111) << 1;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        STRCommon(core, (uint)(core.R[rb] + offset), (ushort)core.R[rd], BusWidth.HalfWord);
    }

    public static void STR_SP_Rel(Core core, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (instruction & 0xFF) << 2;

        STRCommon(core, (uint)(core.R[13] + offset), core.R[rd], BusWidth.Word);
    }

    #endregion

    public static void Get_Rel_PC(Core core, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (instruction & 0xFF) << 2;
        core.R[rd] = (uint)(((core.R[15] + 4) & ~2) + offset);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void Get_Rel_SP(Core core, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (instruction & 0xFF) << 2;
        core.R[rd] = (uint)(core.R[13] + offset);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void ADD_Offset_SP(Core core, ushort instruction)
    {
        var offset = (sbyte)(instruction & 0xFF) << 2;
        core.R[13] = (uint)(core.R[13] + offset);

        core.MoveExecutePipelineToNextInstruction();
    }

    #region PUSH/POP
    private static uint[] _storeLoadMultipleState = new uint[9];
    private static int _storeLoadMultiplePopCount;
    private static int _storeLoadMultiplePtr;

    internal static void PushCycle(Core core, uint instruction)
    {
        if (_storeLoadMultiplePtr >= _storeLoadMultiplePopCount)
        {
            core.R[13] = (uint)(core.R[13] - (_storeLoadMultiplePopCount * 4)); // TODO - Is this the right increment to stack pointer
            
            Core.ResetMemoryUnitForThumbOpcodeFetch(core, instruction);
        }
        else
        {
            core.A += 4;
            core.D = _storeLoadMultipleState[_storeLoadMultiplePtr];
            _storeLoadMultiplePtr++;
        }
    }

    #endregion

    public static void PUSH(Core core, ushort instruction)
    {
        _storeLoadMultiplePopCount = 0;
        _storeLoadMultiplePtr = 0;
        
        var registerList = instruction & 0b1111_1111;
        for (var r = 0; r <= 7; r++)
        {
            if (((registerList >> r) & 0b1) == 0b1)
            {
                _storeLoadMultipleState[_storeLoadMultiplePopCount] = core.R[r];
                _storeLoadMultiplePopCount++;
            }
        }

        // Check push LR
        if (((instruction >> 8) & 0b1) == 1)
        {
            _storeLoadMultipleState[_storeLoadMultiplePopCount] = core.R[14];
            _storeLoadMultiplePopCount++;
        }

        PushCycle(core, instruction);
    }

    internal static void PopCycle(Core core, uint instruction)
    {
        if (_storeLoadMultiplePtr > _storeLoadMultiplePopCount)
        {
            core.R[13] = (uint)(core.R[13] + (_storeLoadMultiplePopCount * 4)); // TODO - Is this the right increment to stack pointer

            Core.ResetMemoryUnitForThumbOpcodeFetch(core, instruction);
        }
        else
        {
            core.A += 4;
            core.R[_storeLoadMultipleState[_storeLoadMultiplePtr]] = core.D;
            if (_storeLoadMultipleState[_storeLoadMultiplePtr] == 15) core.ClearPipeline();
            _storeLoadMultiplePtr++;
        }
    }

    public static void POP(Core core, ushort instruction)
    {
        var registerList = instruction & 0b1111_1111;
        for (var r = 0; r <= 7; r++)
        {
            if (((registerList >> r) & 0b1) == 0b1)
            {
                _storeLoadMultipleState[_storeLoadMultiplePopCount] = (uint)r;
                _storeLoadMultiplePopCount++;
            }
        }

        // Check pop PC
        if (((instruction >> 8) & 0b1) == 1)
        {
            _storeLoadMultipleState[_storeLoadMultiplePopCount] = 15;
            _storeLoadMultiplePopCount++;
        }

        core.NextExecuteAction = &PopCycle;
    }

    internal static void stmia_registerWriteCycle(Core core, uint instruction)
    {
        if (_storeLoadMultiplePtr >= _storeLoadMultiplePopCount)
        {
            var rb = (instruction >> 8) & 0b111;
            core.R[rb] = (uint)(core.R[rb] + (_storeLoadMultiplePopCount * 4));

            Core.ResetMemoryUnitForThumbOpcodeFetch(core, instruction);
        }
        else
        {
            core.A += 4;
            core.D = _storeLoadMultipleState[_storeLoadMultiplePtr];
            _storeLoadMultiplePtr++;
        }
    }

    internal static void ldmia_registerReadCycle(Core core, uint instruction)
    {
        if (_storeLoadMultiplePtr >= _storeLoadMultiplePopCount - 1)
        {
            var rb = (instruction >> 8) & 0b111;
            core.R[rb] = (uint)(core.R[rb] - (_storeLoadMultiplePopCount * 4));

            Core.ResetMemoryUnitForThumbOpcodeFetch(core, instruction);
        }
        else
        {
            core.A -= 4;
            core.R[_storeLoadMultipleState[_storeLoadMultiplePtr]] = core.D;
            _storeLoadMultiplePtr++;
        }
    }
    public static void STMIA(Core core, ushort instruction)
    {
        LDMIA_STMIA_Common(core, instruction);
        core.nRW = true;
        core.NextExecuteAction = &stmia_registerWriteCycle;
        core.A -= 4;
        stmia_registerWriteCycle(core, instruction);
    }

    public static void LDMIA(Core core, ushort instruction)
    {
        LDMIA_STMIA_Common(core, instruction);
        core.nRW = false;
        core.NextExecuteAction = &ldmia_registerReadCycle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void LDMIA_STMIA_Common(Core core, ushort instruction)
    {
        _storeLoadMultiplePopCount = 0;
        _storeLoadMultiplePtr = 0;
        var registerList = instruction & 0b1111_1111;
        var rb = (instruction >> 8) & 0b111;
        for (var r = 0; r <= 7; r++)
        {
            if (((registerList >> r) & 0b1) == 0b1)
            {
                _storeLoadMultipleState[_storeLoadMultiplePopCount] = (uint)r;
                _storeLoadMultiplePopCount++;
            }
        }

        core.nOPC = true;
        core.SEQ = _storeLoadMultiplePopCount > 1;
        core.MAS = BusWidth.Word;
        core.A = core.R[rb];
    }
}
