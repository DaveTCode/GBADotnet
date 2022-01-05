using static GameboyAdvanced.Core.Cpu.ALU;

namespace GameboyAdvanced.Core.Cpu;

/// <summary>
/// This class contains utility methods for the thumb instruction set
/// </summary>
internal unsafe static class Thumb
{
    internal readonly static delegate*<Arm7Tdmi, ushort, int>[] InstructionMap =
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

    public static int LSL_Shift_Reg(Arm7Tdmi cpu, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        cpu.R[rd] = LSL(cpu.R[rs], (byte)offset, ref cpu.Cpsr);

        return 1; // 1S
    }

    public static int LSR_Shift_Reg(Arm7Tdmi cpu, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        cpu.R[rd] = LSR(cpu.R[rs], (byte)offset, ref cpu.Cpsr);

        return 1; // 1S
    }

    public static int ASR_Shift_Reg(Arm7Tdmi cpu, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        cpu.R[rd] = ASR(cpu.R[rs], (byte)offset, ref cpu.Cpsr);

        return 1; // 1S
    }

    public static int ADD_Reg(Arm7Tdmi cpu, ushort instruction)
    {
        var rn = (instruction >> 6) & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        cpu.R[rd] = ADD(cpu.R[rs], cpu.R[rn], ref cpu.Cpsr);

        return 1; // 1S
    }

    public static int ADD_Imm(Arm7Tdmi cpu, ushort instruction)
    {
        var nn = (instruction >> 6) & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        cpu.R[rd] = ADD(cpu.R[rs], (uint)nn, ref cpu.Cpsr);

        return 1; // 1S
    }

    public static int SUB_Reg(Arm7Tdmi cpu, ushort instruction)
    {
        var rn = (instruction >> 6) & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        cpu.R[rd] = SUB(cpu.R[rs], cpu.R[rn], ref cpu.Cpsr);

        return 1; // 1S
    }

    public static int SUB_Imm(Arm7Tdmi cpu, ushort instruction)
    {
        var nn = (instruction >> 6) & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        cpu.R[rd] = SUB(cpu.R[rs], (uint)nn, ref cpu.Cpsr);

        return 1; // 1S
    }

    public static int MOV(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var nn = instruction & 0xFF;

        cpu.R[rd] = (uint)nn;
        SetZeroSignFlags(cpu.Cpsr, cpu.R[rd]);

        return 1; // 1S
    }

    public static int CMP(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var nn = instruction & 0xFF;

        _ = SUB(cpu.R[rd], (uint)nn, ref cpu.Cpsr);

        return 1; // 1S
    }

    public static int ADD_Imm_B(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var nn = instruction & 0xFF;

        cpu.R[rd] = ADD(cpu.R[rd], (uint)nn, ref cpu.Cpsr);

        return 1; // 1S
    }

    public static int SUB_Imm_B(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var nn = instruction & 0xFF;

        cpu.R[rd] = SUB(cpu.R[rd], (uint)nn, ref cpu.Cpsr);

        return 1; // 1S
    }

    public static int ALU(Arm7Tdmi cpu, ushort instruction)
    {
        var opcode = (instruction >> 6) & 0b1111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        switch (opcode)
        {
            case 0x0: // AND
                cpu.R[rd] = cpu.R[rd] & cpu.R[rs];
                SetZeroSignFlags(cpu.Cpsr, cpu.R[rd]);
                return 1; // 1S
            case 0x1: // EOR
                cpu.R[rd] = cpu.R[rd] ^ cpu.R[rs];
                SetZeroSignFlags(cpu.Cpsr, cpu.R[rd]);
                return 1; // 1S
            case 0x2: // LSL
                cpu.R[rd] = LSL(cpu.R[rd], (byte)cpu.R[rs], ref cpu.Cpsr);
                return 2; // 1S 1I
            case 0x3: // LSR
                cpu.R[rd] = LSR(cpu.R[rd], (byte)cpu.R[rs], ref cpu.Cpsr);
                return 2; // 1S 1I
            case 0x4: // ASR
                cpu.R[rd] = ASR(cpu.R[rd], (byte)cpu.R[rs], ref cpu.Cpsr);
                return 2; // 1S 1I
            case 0x5: // ADC
                cpu.R[rd] = ADC(cpu.R[rd], cpu.R[rs], ref cpu.Cpsr);
                return 1; // 1S
            case 0x6: // SBC
                cpu.R[rd] = SBC(cpu.R[rd], cpu.R[rs], ref cpu.Cpsr);
                return 1; // 1S
            case 0x7: // ROR
                cpu.R[rd] = ROR(cpu.R[rd], (byte)cpu.R[rs], ref cpu.Cpsr);
                return 2; // 1S 1I
            case 0x8: // TST
                var result = cpu.R[rd] & cpu.R[rs];
                SetZeroSignFlags(cpu.Cpsr, result);
                return 1; // 1S
            case 0x9: // NEG
                cpu.R[rd] = SUB(0, cpu.R[rs], ref cpu.Cpsr);
                return 1; // 1S
            case 0xA: // CMP
                _ = SUB(0, cpu.R[rs], ref cpu.Cpsr);
                return 1; // 1S
            case 0xB: // CMN
                _ = ADD(0, cpu.R[rs], ref cpu.Cpsr);
                return 1; // 1S
            case 0xC: // ORR
                cpu.R[rd] = cpu.R[rd] | cpu.R[rs];
                SetZeroSignFlags(cpu.Cpsr, cpu.R[rd]);
                return 1; // 1S
            case 0xD: // MUL
                cpu.R[rd] = MUL(cpu.R[rd], cpu.R[rs], ref cpu.Cpsr);
                return 5; // 1S + (1..4)I - TODO - Just setting to max for now
            case 0xE: // BIC
                cpu.R[rd] = cpu.R[rd] & (~cpu.R[rs]);
                SetZeroSignFlags(cpu.Cpsr, cpu.R[rd]);
                return 1; // 1S
            case 0xF: // MVN
                cpu.R[rd] = ~cpu.R[rs];
                SetZeroSignFlags(cpu.Cpsr, cpu.R[rd]);
                return 1; // 1S
            default:
                throw new NotImplementedException($"Thumb ALU opcode {opcode:X2} not implemented");
        }
    }

    public static int HiReg_Or_BX(Arm7Tdmi cpu, ushort instruction)
    {
        var opcode = (instruction >> 8) & 0b11;
        var msbd = (instruction >> 7) & 0b1;
        var msbs = (instruction >> 6) & 0b1;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        var fullRd = rd | (msbd << 4);
        var fullRs = rs | (msbs << 4);

        var operand = fullRs switch
        {
            15 => (cpu.R[15] + 4) & 0xFFFF_FFFE,
            _ => cpu.R[fullRs]
        };

        switch (opcode)
        {
            // TODO - "Restrictions: For ADD/CMP/MOV, MSBs and/or MSBd must be set, ie. it is not allowed that both are cleared." - ok, but what happens if they're not?
            // ADD
            case 0b00:
                cpu.R[fullRd] = cpu.R[fullRd] + operand;
                return (fullRd == 15) ? 3 : 1;
            // CMP
            case 0b01:
                _ = SUB(cpu.R[fullRd], operand, ref cpu.Cpsr);
                return 1; // 1S
            // MOV/NOP
            case 0b10:
                cpu.R[fullRd] = operand;
                return (fullRd == 15) ? 3 : 1;
            // BX/BLX
            case 0b11:
                cpu.Cpsr.ThumbMode = (cpu.R[fullRs] & 0b1) == 1;
                cpu.R[15] = cpu.Cpsr.ThumbMode ? cpu.R[fullRs] & 0xFFFF_FFFE : cpu.R[fullRs] & 0xFFFF_FFFC;

                // TODO - http://www.problemkaputt.de/gbatek.htm#thumbinstructionsummary suggests that CLX happens if MSBd is set, other docs don't
                return 3; // 2S + 1N
            default:
                throw new Exception("Invalid");
        }
    }

    public static int LDR_PC_Offset(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var word = instruction & 0xFF;
        var address = ((cpu.R[15] + 4) & ~3) + word;

        (cpu.R[rd], var cycles) = cpu.Bus.ReadWord((uint)address);

        return 3 + cycles; // 1S 1N 1I + memory bus cycles TODO mayby the 1N is this non-sequential bus access?
    }

    public static int STR_Reg_Offset(Arm7Tdmi cpu, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        var address = cpu.R[rb] + cpu.R[ro];
        var cycles = cpu.Bus.WriteWord(address, cpu.R[rd]);

        return 2 + cycles; // 2N + memory bus cycles
    }

    public static int LDR_Reg_Offset(Arm7Tdmi cpu, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        var address = cpu.R[rb] + cpu.R[ro];
        (cpu.R[rd], var cycles) = cpu.Bus.ReadWord(address);

        return 3 + cycles; // 1S + 1N + 1I + memory bus cycles
    }

    public static int STRB_Reg_Offset(Arm7Tdmi cpu, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        var address = cpu.R[rb] + cpu.R[ro];
        var cycles = cpu.Bus.WriteByte(address, (byte)cpu.R[rd]);

        return 2 + cycles; // 2N + memory bus cycles
    }

    public static int LDRB_Reg_Offset(Arm7Tdmi cpu, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        var address = cpu.R[rb] + cpu.R[ro];
        (cpu.R[rd], var cycles) = cpu.Bus.ReadByte(address);

        return 3 + cycles; // 1S + 1N + 1I + memory bus cycles
    }

    public static int STRH_Reg_Offset(Arm7Tdmi cpu, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        var address = cpu.R[rb] + cpu.R[ro];
        var cycles = cpu.Bus.WriteHalfWord(address, (ushort)cpu.R[rd]);

        return 2 + cycles; // 2N + memory bus cycles
    }

    public static int LDSB_Reg_Offset(Arm7Tdmi cpu, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        var address = cpu.R[rb] + cpu.R[ro];
        var (data, cycles) = cpu.Bus.ReadByte(address);

        cpu.R[rd] = (uint)((sbyte)data);

        return 3 + cycles; // 1S + 1N + 1I + memory bus cycles
    }

    public static int LDRH_Reg_Offset(Arm7Tdmi cpu, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        var address = cpu.R[rb] + cpu.R[ro];
        var (data, cycles) = cpu.Bus.ReadHalfWord(address);

        cpu.R[rd] = data;

        return 3 + cycles; // 1S + 1N + 1I + memory bus cycles
    }

    public static int LDSH_Reg_Offset(Arm7Tdmi cpu, ushort instruction)
    {
        var ro = (instruction >> 6) & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;
        var address = cpu.R[rb] + cpu.R[ro];
        var (data, cycles) = cpu.Bus.ReadHalfWord(address);

        cpu.R[rd] = (uint)((short)data);

        return 3 + cycles; // 1S + 1N + 1I + memory bus cycles
    }

    public static int STR_Imm_Offset(Arm7Tdmi cpu, ushort instruction)
    {
        // For word accesses (B = 0), the value specified by #Imm is a full 7-bit address, but must
        // be word-aligned(ie with bits 1:0 set to 0), since the assembler places #Imm >> 2 in
        // the Offset5 field
        var offset = ((instruction >> 6) & 0b1_1111) << 2;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        var address = (uint)(cpu.R[rb] + offset);

        var cycles = cpu.Bus.WriteWord(address, cpu.R[rd]);

        return 2 + cycles; // 2N + memory bus cycles
    }

    public static int LDR_Imm_Offset(Arm7Tdmi cpu, ushort instruction)
    {
        var offset = ((instruction >> 6) & 0b1_1111) << 2;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        var address = (uint)(cpu.R[rb] + offset);

        (cpu.R[rd], var cycles) = cpu.Bus.ReadWord(address);

        return 3 + cycles; // 1S + 1N + 1I + memory bus cycles
    }

    public static int STRB_Imm_Offset(Arm7Tdmi cpu, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        var address = (uint)(cpu.R[rb] + offset);

        var cycles = cpu.Bus.WriteByte(address, (byte)cpu.R[rd]);

        return 2 + cycles; // 2N + memory bus cycles
    }

    public static int LDRB_Imm_Offset(Arm7Tdmi cpu, ushort instruction)
    {
        var offset = (instruction >> 6) & 0b1_1111;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        var address = (uint)(cpu.R[rb] + offset);

        (cpu.R[rd], var cycles) = cpu.Bus.ReadByte(address);

        return 3 + cycles; // 1S + 1N + 1I + memory bus cycles
    }

    public static int STRH(Arm7Tdmi cpu, ushort instruction)
    {
        // #Imm is a full 6-bit address but must be halfword-aligned (ie with bit 0 set to 0) since
        // the assembler places #Imm >> 1 in the Offset5 field
        var offset = ((instruction >> 6) & 0b1_1111) << 1;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        var address = (uint)(cpu.R[rb] + offset);

        var cycles = cpu.Bus.WriteHalfWord(address, (ushort)cpu.R[rd]);

        return 2 + cycles; // 2N + memory bus cycles
    }

    public static int LDRH(Arm7Tdmi cpu, ushort instruction)
    {
        var offset = ((instruction >> 6) & 0b1_1111) << 1;
        var rb = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        var address = (uint)(cpu.R[rb] + offset);

        (cpu.R[rd], var cycles) = cpu.Bus.ReadHalfWord(address);

        return 3 + cycles; // 1S + 1N + 1I + memory bus cycles
    }

    public static int STR_SP_Rel(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (instruction & 0xFF) << 2;
        var address = (uint)(cpu.R[13] + offset);

        var cycles = cpu.Bus.WriteWord(address, cpu.R[rd]);

        return 2 + cycles; // 2N
    }

    public static int LDR_SP_Rel(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (instruction & 0xFF) << 2;
        var address = (uint)(cpu.R[13] + offset);

        (cpu.R[rd], var cycles) = cpu.Bus.ReadWord(address);

        return 3 + cycles; // 1S + 1N + 1I + memory bus cycles
    }

    public static int Get_Rel_PC(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (instruction & 0xFF) << 2;
        cpu.R[rd] = (uint)(((cpu.R[15] + 4) & ~2) + offset);

        return 1; // 1S
    }

    public static int Get_Rel_SP(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (instruction & 0xFF) << 2;
        cpu.R[rd] = (uint)(cpu.R[13] + offset);

        return 1; // 1S
    }

    public static int ADD_Offset_SP(Arm7Tdmi cpu, ushort instruction)
    {
        var offset = (sbyte)(instruction & 0xFF) << 2;
        cpu.R[13] = (uint)(cpu.R[13] + offset);

        return 1; // 1S
    }

    public static int PUSH(Arm7Tdmi cpu, ushort instruction)
    {
        int cycles = 2; // 2N
        int pushes = 0;

        // Check push LR
        if (((instruction >> 8) & 0b1) == 1)
        {
            cpu.R[13] -= 4;
            cycles += cpu.Bus.WriteWord(cpu.R[13], cpu.R[14]);
            pushes++;
        }

        for (var i = 7; i >= 0; i--)
        {
            if (((instruction >> i) & 0b1) == 1)
            {
                cpu.R[13] -= 4;
                cycles += cpu.Bus.WriteWord(cpu.R[13], cpu.R[i]);
                pushes++;
            }
        }

        return cycles + (pushes - 1); // 2N + (n-1)S
    }

    public static int POP(Arm7Tdmi cpu, ushort instruction)
    {
        int cycles = 2; // 1N + 1I

        for (var i = 0; i < 8; i++)
        {
            if (((instruction >> i) & 0b1) == 1)
            {
                (cpu.R[i], var nCycles) = cpu.Bus.ReadWord(cpu.R[13]);
                cycles += nCycles;
                cpu.R[13] += 4;
            }
        }

        if (((instruction >> 8) & 0b1) == 1)
        {
            (cpu.R[15], var nCycles) = cpu.Bus.ReadWord(cpu.R[13]);
            cpu.R[15] &= 0xFFFF_FFFE; // Bit 0 is ignored on POP (can't switch to ARM mode through POP)
            cycles += nCycles;
            cpu.R[13] += 4;
        }

        return cycles;
    }

    public static int STMIA(Arm7Tdmi cpu, ushort instruction)
    {
        throw new NotImplementedException();
    }

    public static int LDMIA(Arm7Tdmi cpu, ushort instruction)
    {
        throw new NotImplementedException();
    }
}
