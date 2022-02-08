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
        &LSL_Imm, &LSL_Imm, &LSL_Imm, &LSL_Imm, &LSL_Imm, &LSL_Imm, &LSL_Imm, &LSL_Imm,
        // LSR{S} Rd,Rs,#Offset - 0x08 - 0x0F
        &LSR_Imm, &LSR_Imm, &LSR_Imm, &LSR_Imm, &LSR_Imm, &LSR_Imm, &LSR_Imm, &LSR_Imm,
        // LSR{S} Rd,Rs,#Offset - 0x10 - 0x17
        &ASR_Imm, &ASR_Imm, &ASR_Imm, &ASR_Imm, &ASR_Imm, &ASR_Imm, &ASR_Imm, &ASR_Imm,
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
        // LDR/STR  Rd,[Rb,Ro]      - 0x50 - 0x5F
        &STR_Reg_Offset, &STR_Reg_Offset,
        &STRH_Reg_Offset, &STRH_Reg_Offset,
        &STRB_Reg_Offset, &STRB_Reg_Offset,
        &LDSB_Reg_Offset, &LDSB_Reg_Offset,
        &LDR_Reg_Offset, &LDR_Reg_Offset,
        &LDRH_Reg_Offset, &LDRH_Reg_Offset,
        &LDRB_Reg_Offset, &LDRB_Reg_Offset,
        &LDSH_Reg_Offset, &LDSH_Reg_Offset,
        // STR  Rd,[Rb,#nn]
        &STR_Imm, &STR_Imm, &STR_Imm, &STR_Imm, &STR_Imm, &STR_Imm, &STR_Imm, &STR_Imm,
        // LDR  Rd,[Rb,#nn]
        &LDR_Imm, &LDR_Imm, &LDR_Imm, &LDR_Imm, &LDR_Imm, &LDR_Imm, &LDR_Imm, &LDR_Imm,
        // STRB Rd,[Rb,#nn]
        &STRB_Imm, &STRB_Imm, &STRB_Imm, &STRB_Imm, &STRB_Imm, &STRB_Imm, &STRB_Imm, &STRB_Imm,
        // LDRB Rd,[Rb,#nn]
        &LDRB_Imm, &LDRB_Imm, &LDRB_Imm, &LDRB_Imm, &LDRB_Imm, &LDRB_Imm, &LDRB_Imm, &LDRB_Imm,
        // STRH Rd,[Rb,#nn]
        &STRH_Imm, &STRH_Imm, &STRH_Imm, &STRH_Imm, &STRH_Imm, &STRH_Imm, &STRH_Imm, &STRH_Imm,
        // LDRH Rd,[Rb,#nn]
        &LDRH_Imm, &LDRH_Imm, &LDRH_Imm, &LDRH_Imm, &LDRH_Imm, &LDRH_Imm, &LDRH_Imm, &LDRH_Imm,
        // STR  Rd,[SP,#nn]
        &STR_SP_Rel, &STR_SP_Rel, &STR_SP_Rel, &STR_SP_Rel, &STR_SP_Rel, &STR_SP_Rel, &STR_SP_Rel, &STR_SP_Rel,
        // LDR  Rd,[SP,#nn]
        &LDR_SP_Rel, &LDR_SP_Rel, &LDR_SP_Rel, &LDR_SP_Rel, &LDR_SP_Rel, &LDR_SP_Rel, &LDR_SP_Rel, &LDR_SP_Rel,
        // ADD  Rd,PC,#nn
        &Get_Rel_PC, &Get_Rel_PC, &Get_Rel_PC, &Get_Rel_PC, &Get_Rel_PC, &Get_Rel_PC, &Get_Rel_PC, &Get_Rel_PC,
        // ADD  Rd,SP,#nn
        &Get_Rel_SP, &Get_Rel_SP, &Get_Rel_SP, &Get_Rel_SP, &Get_Rel_SP, &Get_Rel_SP, &Get_Rel_SP, &Get_Rel_SP,
        // ADD  SP,#nn, ADD  SP,#-nn
        &ADD_Offset_SP, &Undefined, &Undefined, &Undefined,
        // PUSH {Rlist}{LR}
        &PUSH, &PUSH, &Undefined, &Undefined, &Undefined, &Undefined, &Undefined, &Undefined,
        // POP  {Rlist}{LR}
        &POP, &POP, &Undefined, &Undefined,
        // STMIA Rb!,{Rlist}
        &STMIA, &STMIA, &STMIA, &STMIA, &STMIA, &STMIA, &STMIA, &STMIA,
        // LDMIA Rb!,{Rlist}
        &LDMIA, &LDMIA, &LDMIA, &LDMIA, &LDMIA, &LDMIA, &LDMIA, &LDMIA,
        // Branch conditionals
        &BEQ, &BNE, &BCS, &BCC, &BMI, &BPL, &BVS, &BVC,
        &BHI, &BLS, &BGE, &BLT, &BGT, &BLE, &Undefined, &SWI,
        &B,&B,&B,&B,&B,&B,&B,&B,
        &Undefined, &Undefined, &Undefined, &Undefined,&Undefined, &Undefined, &Undefined, &Undefined,
        &BL_Low, &BL_Low, &BL_Low, &BL_Low, &BL_Low, &BL_Low, &BL_Low, &BL_Low,
        &BL_Hi, &BL_Hi, &BL_Hi, &BL_Hi, &BL_Hi, &BL_Hi, &BL_Hi, &BL_Hi
    };

    #region Branches
    private static void BranchCommon(Core core, int offset)
    {
        core.R[15] = (uint)(core.R[15] + offset);
        core.A = core.R[15];
        core.ClearPipeline(); // Note that this will trigger two more cycles (both just fetches, with nothing to execute)

        core.MoveExecutePipelineToNextInstruction();
    }

    private static void BranchConditional(Core core, ushort instruction, bool flag, bool val)
    {
        if (flag == val)
        {
            var offset = ((short)((instruction & 0b1111_1111) << 8)) >> 7;
            BranchCommon(core, offset);
        }
        else
        {
            core.MoveExecutePipelineToNextInstruction();
        }
    }

    public static void B(Core core, ushort instruction)
    {
        var offset = ((short)((instruction & 0b111_1111_1111) << 5)) >> 4;
        BranchCommon(core, offset);
    }

    public static void BEQ(Core core, ushort instruction) => BranchConditional(core, instruction, core.Cpsr.ZeroFlag, true);
    public static void BNE(Core core, ushort instruction) => BranchConditional(core, instruction, core.Cpsr.ZeroFlag, false);
    public static void BCS(Core core, ushort instruction) => BranchConditional(core, instruction, core.Cpsr.CarryFlag, true);
    public static void BCC(Core core, ushort instruction) => BranchConditional(core, instruction, core.Cpsr.CarryFlag, false);
    public static void BMI(Core core, ushort instruction) => BranchConditional(core, instruction, core.Cpsr.SignFlag, true);
    public static void BPL(Core core, ushort instruction) => BranchConditional(core, instruction, core.Cpsr.SignFlag, false);
    public static void BVS(Core core, ushort instruction) => BranchConditional(core, instruction, core.Cpsr.OverflowFlag, true);
    public static void BVC(Core core, ushort instruction) => BranchConditional(core, instruction, core.Cpsr.OverflowFlag, false);
    public static void BHI(Core core, ushort instruction) => BranchConditional(core, instruction, core.Cpsr.CarryFlag && !core.Cpsr.ZeroFlag, true);
    public static void BLS(Core core, ushort instruction) => BranchConditional(core, instruction, !core.Cpsr.CarryFlag || core.Cpsr.ZeroFlag, true);
    public static void BGE(Core core, ushort instruction) => BranchConditional(core, instruction, core.Cpsr.SignFlag == core.Cpsr.OverflowFlag, true);
    public static void BLT(Core core, ushort instruction) => BranchConditional(core, instruction, core.Cpsr.SignFlag != core.Cpsr.OverflowFlag, true);
    public static void BGT(Core core, ushort instruction) => BranchConditional(core, instruction, !core.Cpsr.ZeroFlag && (core.Cpsr.SignFlag == core.Cpsr.OverflowFlag), true);
    public static void BLE(Core core, ushort instruction) => BranchConditional(core, instruction, core.Cpsr.ZeroFlag || (core.Cpsr.SignFlag != core.Cpsr.OverflowFlag), true);
    
    public static void BL_Low(Core core, ushort instruction)
    {
        var offset = ((int)((instruction & 0b111_1111_1111) << 21)) >> 9;
        core.R[14] = (uint)(core.R[15] + offset);
    }

    public static void BL_Hi(Core core, ushort instruction)
    {
        var offset = (instruction & 0b111_1111_1111) << 1;
        var newPc = (uint)(core.R[14] + offset);
        core.R[14] = (core.R[15] - 2) | 1;
        core.R[15] = newPc;
        core.A = core.R[15];
        core.ClearPipeline();

        core.MoveExecutePipelineToNextInstruction();
    }
    #endregion

    public static void SWI(Core _core, ushort _instruction) => throw new NotImplementedException("SWI Thumb not implemented");

    public static void Undefined(Core _core, ushort _instruction) => throw new NotImplementedException();

    public static void LSL_Imm(Core core, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        core.R[rd] = LSL(core.R[rs], (byte)offset, ref core.Cpsr);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void LSR_Imm(Core core, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        core.R[rd] = LSR(core.R[rs], (byte)offset, ref core.Cpsr);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void ASR_Imm(Core core, ushort instruction)
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

    public static void LDRB_Imm(Core core, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rb = (instruction >> 3) & 0b111;
        LDRCommon(core, (uint)(core.R[rb] + offset), BusWidth.Byte, instruction & 0b111, &LDRB);
    }

    public static void LDRH_Imm(Core core, ushort instruction)
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

    public static void LDR_Imm(Core core, ushort instruction)
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

    public static void STR_Imm(Core core, ushort instruction)
    {
        // For word accesses (B = 0), the value specified by #Imm is a full 7-bit address, but must
        // be word-aligned(ie with bits 1:0 set to 0), since the assembler places #Imm >> 2 in
        // the Offset5 field
        var offset = ((instruction >> 6) & 0b1_1111) << 2;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        STRCommon(core, (uint)(core.R[rb] + offset), core.R[rd], BusWidth.Word);
    }

    public static void STRB_Imm(Core core, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        STRCommon(core, (uint)(core.R[rb] + offset), (byte)core.R[rd], BusWidth.Byte);
    }

    public static void STRH_Imm(Core core, ushort instruction)
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
