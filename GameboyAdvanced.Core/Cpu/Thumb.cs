using GameboyAdvanced.Core.Cpu.Shared;
using static GameboyAdvanced.Core.Cpu.ALU;
using static GameboyAdvanced.Core.Cpu.Shifter;

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
        #if DEBUG
        if ((uint)(core.R[15] + offset) == 0)
        {
            core.Debugger.FireEvent(Debug.DebugEvent.BranchToZero, core);
        }
        #endif
        core.R[15] = (uint)(core.R[15] + offset);
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
#if DEBUG
        if (newPc == 0)
        {
            core.Debugger.FireEvent(Debug.DebugEvent.BranchToZero, core);
        }
#endif
        core.R[14] = (core.R[15] - 2) | 1;
        core.R[15] = newPc;
        core.ClearPipeline();

        core.MoveExecutePipelineToNextInstruction();
    }
    #endregion

    public static void SWI(Core core, ushort _instruction)
    {
        core.HandleInterrupt(0x08u, core.R[15] - 2, CPSRMode.Supervisor);
        core.MoveExecutePipelineToNextInstruction();
    }

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

        core.R[rd] = LSRImmediate(core.R[rs], (byte)offset, ref core.Cpsr);

        core.MoveExecutePipelineToNextInstruction();
    }

    public static void ASR_Imm(Core core, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        core.R[rd] = ASRImmediate(core.R[rs], (byte)offset, ref core.Cpsr);

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
                core.WaitStates++; // TODO - Treat as proper extra I cycle
                break;
            case 0x3: // LSR
                core.R[rd] = LSRRegister(core.R[rd], (byte)core.R[rs], ref core.Cpsr);
                core.WaitStates++; // TODO - Treat as proper extra I cycle
                break;
            case 0x4: // ASR
                core.R[rd] = ASRRegister(core.R[rd], (byte)core.R[rs], ref core.Cpsr);
                core.WaitStates++; // TODO - Treat as proper extra I cycle
                break;
            case 0x5: // ADC
                core.R[rd] = ADC(core.R[rd], core.R[rs], ref core.Cpsr);
                break;
            case 0x6: // SBC
                core.R[rd] = SBC(core.R[rd], core.R[rs], ref core.Cpsr);
                break;
            case 0x7: // ROR
                core.R[rd] = RORRegister(core.R[rd], (byte)core.R[rs], ref core.Cpsr);
                core.WaitStates++; // TODO - Treat as proper extra I cycle
                break;
            case 0x8: // TST
                var result = core.R[rd] & core.R[rs];
                SetZeroSignFlags(ref core.Cpsr, result);
                break;
            case 0x9: // NEG
                core.R[rd] = SUB(0, core.R[rs], ref core.Cpsr);
                break;
            case 0xA: // CMP
                _ = SUB(core.R[rd], core.R[rs], ref core.Cpsr);
                break;
            case 0xB: // CMN
                _ = ADD(core.R[rd], core.R[rs], ref core.Cpsr);
                break;
            case 0xC: // ORR
                core.R[rd] = core.R[rd] | core.R[rs];
                SetZeroSignFlags(ref core.Cpsr, core.R[rd]);
                break;
            case 0xD: // MUL
                MultiplyUtils.SetupForMultiplyFlags(core, rd, rs, rd);
                break;
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
            15 => core.R[15] & 0xFFFF_FFFE,
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
                core.ClearPipeline();

                // TODO - http://www.problemkaputt.de/gbatek.htm#thumbinstructionsummary suggests that CLX happens if MSBd is set, other docs don't
                core.MoveExecutePipelineToNextInstruction();
                break;
            default:
                throw new Exception("Invalid");
        }
    }

    #region Load Register

    public static void LDR_PC_Offset(Core core, ushort instruction)
    {
        var word = instruction & 0xFF;
        LdrStrUtils.LDRCommon(core, (uint)((core.R[15] & ~3) + (word << 2)), BusWidth.Word, (instruction >> 8) & 0b111, &LdrStrUtils.LDRW);
    }

    public static void LDR_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        LdrStrUtils.LDRCommon(core, core.R[rb] + core.R[ro], BusWidth.Word, instruction & 0b111, &LdrStrUtils.LDRW);
    }

    public static void LDRB_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        LdrStrUtils.LDRCommon(core, core.R[rb] + core.R[ro], BusWidth.Byte, instruction & 0b111, &LdrStrUtils.LDRB);
    }

    public static void LDSB_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        LdrStrUtils.LDRCommon(core, core.R[rb] + core.R[ro], BusWidth.Byte, instruction & 0b111, &LdrStrUtils.LDRSB);
    }

    public static void LDRH_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        LdrStrUtils.LDRCommon(core, core.R[rb] + core.R[ro], BusWidth.HalfWord, instruction & 0b111, &LdrStrUtils.LDRHW);
    }

    public static void LDSH_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        LdrStrUtils.LDRCommon(core, core.R[rb] + core.R[ro], BusWidth.HalfWord, instruction & 0b111, &LdrStrUtils.LDRSHW);
    }

    public static void LDRB_Imm(Core core, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rb = (instruction >> 3) & 0b111;
        LdrStrUtils.LDRCommon(core, (uint)(core.R[rb] + offset), BusWidth.Byte, instruction & 0b111, &LdrStrUtils.LDRB);
    }

    public static void LDRH_Imm(Core core, ushort instruction)
    {
        var offset = ((instruction >> 6) & 0b1_1111) << 1;
        var rb = (instruction >> 3) & 0b111;
        LdrStrUtils.LDRCommon(core, (uint)(core.R[rb] + offset), BusWidth.HalfWord, instruction & 0b111, &LdrStrUtils.LDRHW);
    }

    public static void LDR_SP_Rel(Core core, ushort instruction)
    {
        var offset = (instruction & 0xFF) << 2;
        LdrStrUtils.LDRCommon(core, (uint)(core.R[13] + offset), BusWidth.Word, (instruction >> 8) & 0b111, &LdrStrUtils.LDRW);
    }

    public static void LDR_Imm(Core core, ushort instruction)
    {
        var offset = ((instruction >> 6) & 0b1_1111) << 2;
        var rb = (instruction >> 3) & 0b111;
        LdrStrUtils.LDRCommon(core, (uint)(core.R[rb] + offset), BusWidth.Word, instruction & 0b111, &LdrStrUtils.LDRW);
    }

    #endregion

    #region Store Register

    public static void STR_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        LdrStrUtils.STRCommon(core, core.R[rb] + core.R[ro], core.R[rd], BusWidth.Word);
    }

    public static void STRB_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        LdrStrUtils.STRCommon(core, core.R[rb] + core.R[ro], (byte)core.R[rd], BusWidth.Byte);
    }

    public static void STRH_Reg_Offset(Core core, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        LdrStrUtils.STRCommon(core, core.R[rb] + core.R[ro], (ushort)core.R[rd], BusWidth.HalfWord);
    }

    public static void STR_Imm(Core core, ushort instruction)
    {
        // For word accesses (B = 0), the value specified by #Imm is a full 7-bit address, but must
        // be word-aligned(ie with bits 1:0 set to 0), since the assembler places #Imm >> 2 in
        // the Offset5 field
        var offset = ((instruction >> 6) & 0b1_1111) << 2;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        LdrStrUtils.STRCommon(core, (uint)(core.R[rb] + offset), core.R[rd], BusWidth.Word);
    }

    public static void STRB_Imm(Core core, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        LdrStrUtils.STRCommon(core, (uint)(core.R[rb] + offset), (byte)core.R[rd], BusWidth.Byte);
    }

    public static void STRH_Imm(Core core, ushort instruction)
    {
        // #Imm is a full 6-bit address but must be halfword-aligned (ie with bit 0 set to 0) since
        // the assembler places #Imm >> 1 in the Offset5 field
        var offset = ((instruction >> 6) & 0b1_1111) << 1;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        LdrStrUtils.STRCommon(core, (uint)(core.R[rb] + offset), (ushort)core.R[rd], BusWidth.HalfWord);
    }

    public static void STR_SP_Rel(Core core, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (instruction & 0xFF) << 2;

        LdrStrUtils.STRCommon(core, (uint)(core.R[13] + offset), core.R[rd], BusWidth.Word);
    }

    #endregion

    public static void Get_Rel_PC(Core core, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (instruction & 0xFF) << 2;
        core.R[rd] = (uint)((core.R[15] & ~2) + offset);

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
        var sign = ((instruction >> 7) & 0b1) * -1;
        var val = (instruction & 0b111_1111) << 2;
        core.R[13] = (uint)(core.R[13] + (sign * val));

        core.MoveExecutePipelineToNextInstruction();
    }

    #region Load/Store/Push/Pop multiple
    private static void LoadStoreMultipleCommon(Core core, ushort instruction, uint initialAddress, bool nRW, uint writebackReg, delegate*<Core, uint, void> nextAction)
    {
        LdmStmUtils.Reset();
        LdmStmUtils._storeLoadMultipleDoWriteback = true;
        var registerList = instruction & 0b1111_1111;

        for (var r = 0; r <= 7; r++)
        {
            if (((registerList >> r) & 0b1) == 0b1)
            {
                LdmStmUtils._storeLoadMultipleState[LdmStmUtils._storeLoadMultiplePopCount] = (uint)r;
                LdmStmUtils._storeLoadMultiplePopCount++;
            }
        }

        core.nOPC = true;
        core.SEQ = LdmStmUtils._storeLoadMultiplePopCount > 1;
        core.MAS = BusWidth.Word;
        core.A = initialAddress;
        core.nRW = nRW;
        core.NextExecuteAction = nextAction;
        LdmStmUtils._writebackRegister = (int)writebackReg;
    }

    public static void PUSH(Core core, ushort instruction)
    {
        LoadStoreMultipleCommon(core, instruction, core.R[13], true, 13, &LdmStmUtils.stm_registerWriteCycle);

        // Check push LR
        if (((instruction >> 8) & 0b1) == 1)
        {
            LdmStmUtils._storeLoadMultipleState[LdmStmUtils._storeLoadMultiplePopCount] = 14;
            LdmStmUtils._storeLoadMultiplePopCount++;
        }

        core.A = (uint)(core.R[13] - (4 * (LdmStmUtils._storeLoadMultiplePopCount + 1)));
        LdmStmUtils._storeLoadMutipleFinalWritebackValue = core.A + 4;

        LdmStmUtils.stm_registerWriteCycle(core, instruction);
    }

    /// <summary>
    /// POP is equivalent to LDMIA R13! for ARM but can only include the 
    /// bottom 8 registers and optionally R15.
    /// </summary>
    public static void POP(Core core, ushort instruction)
    {
        LoadStoreMultipleCommon(core, instruction, core.R[13], false, 13, &LdmStmUtils.ldm_registerReadCycle);

        // Check pop PC
        if (((instruction >> 8) & 0b1) == 1)
        {
            LdmStmUtils._storeLoadMultipleState[LdmStmUtils._storeLoadMultiplePopCount] = 15;
            LdmStmUtils._storeLoadMultiplePopCount++;
        }

        LdmStmUtils._storeLoadMutipleFinalWritebackValue = (uint)(core.R[13] + (4 * LdmStmUtils._storeLoadMultiplePopCount));
    }

    public static void STMIA(Core core, ushort instruction)
    {
        var rb = (uint)((instruction >> 8) & 0b111);
        LoadStoreMultipleCommon(core, instruction, core.R[rb] - 4, true, rb, &LdmStmUtils.stm_registerWriteCycle);
        LdmStmUtils._storeLoadMutipleFinalWritebackValue = (uint)(core.R[rb] + (4 * LdmStmUtils._storeLoadMultiplePopCount));
        LdmStmUtils.stm_registerWriteCycle(core, instruction);
    }

    public static void LDMIA(Core core, ushort instruction)
    {
        var rb = (uint)((instruction >> 8) & 0b111);
        LoadStoreMultipleCommon(core, instruction, core.R[rb], false, rb, &LdmStmUtils.ldm_registerReadCycle);
        LdmStmUtils._storeLoadMutipleFinalWritebackValue = (uint)(core.R[rb] + (4 * LdmStmUtils._storeLoadMultiplePopCount));
    }

    #endregion
}
