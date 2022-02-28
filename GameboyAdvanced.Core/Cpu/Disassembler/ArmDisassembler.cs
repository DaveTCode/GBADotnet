﻿using static GameboyAdvanced.Core.Cpu.Disassembler.Utils;

namespace GameboyAdvanced.Core.Cpu.Disassembler;

internal static class ArmDisassembler
{
    internal static string Disassemble(Core core, uint instruction)
    {
        var condCode = (instruction >> 28) & 0b1111;

        var condString = condCode switch
        {
            0 => "EQ",
            1 => "NE",
            2 => "CS",
            3 => "CC",
            4 => "MI",
            5 => "PL",
            6 => "VS",
            7 => "VC",
            8 => "HI",
            9 => "LS",
            0xA => "GE",
            0xB => "LT",
            0xC => "GT",
            0xD => "LE",
            0xE => "",
            0xF => "XXXX", // TODO - "never (ARMv1,v2 only) (Reserved ARMv3 and up)" - ok but what happens if it _is_ set
            _ => throw new Exception("Invalid state"),
        };

        var instructionStr = instruction switch
        {
            var i when ((i >> 22) & 0b11_1111) == 0 && ((i >> 4) & 0b1111) == 0b1001 => DisassembleArmMultiply(core, i),
            var i when ((i >> 23) & 0b1_1111) == 1 && ((i >> 4) & 0b1111) == 0b1001 => DisassembleArmMultiplyLong(core, i),
            var i when ((i >> 23) & 0b1_1111) == 0b0_0010 && ((i >> 20) & 0b11) == 0b00 && ((i >> 4) & 0b1111_1111) == 0b0000_1001 => DisassembleArmSingleDataSwap(core, i),
            var i when ((i >> 4) & 0b1111_1111_1111_1111_1111_1111) == 0b0001_0010_1111_1111_1111_0001 => DisassembleArmBranchEx(core, i),
            var i when ((i >> 25) & 0b111) == 0b000 && ((i >> 22) & 0b1) == 0 && ((i >> 7) & 0b1_1111) == 0b0_0001 && ((i >> 4) & 0b1) == 1 => DisassembleArmHalfWordRegOffset (core, i),
            var i when ((i >> 25) & 0b111) == 0b000 && ((i >> 22) & 0b1) == 1 && ((i >> 7) & 0b1) == 1 && ((i >> 4) & 0b1) == 1 => DisassembleArmHalfWordImmOffset(core, i),
            var i when ((i >> 23) & 0b1_1111) == 0b0_0010 => DisassemblePsr(core, i),
            var i when ((i >> 23) & 0b1_1111) == 0b0_0110 => DisassemblePsr(core, i),
            var i when ((i >> 26) & 0b11) == 0b00 => DisassembleArmDataOp(core, i),
            var i when ((i >> 26) & 0b11) == 0b01 => ((i >> 4) & 0b1) switch
            {
                0b0 => DisassembleArmSingleDataTransfer(core, i),
                0b1 => "Undefined",
                _ => throw new Exception(),
            },
            var i when ((i >> 25) & 0b111) == 0b100 => DisassembleArmBlockDataTransfer(core, i),
            var i when ((i >> 25) & 0b111) == 0b101 => DisassembleArmBranch(core, i),
            var i when ((i >> 25) & 0b111) == 0b110 => DisassembleArmCoProcessorDataTransfer(core, i),
            var i when ((i >> 24) & 0b1111) == 0b1110 => ((i >> 4) & 0b1) switch
            {
                0b0 => DisassembleArmCoprocessorDataOp(core, i),
                0b1 => DisassembleArmCoprocessorRegTransfer(core, i),
                _ => throw new Exception(),
            },
            var i when ((i >> 24) & 0b1111) == 0b1111 => DisassembleArmSwi(core, i),
            _ => throw new Exception($"Invalid instruction {instruction:X8}"),
        };

        return string.Format(instructionStr, condString);
    }

    private static string DisassemblePsr(Core core, uint instruction)
    {
        var isMsr = ((instruction >> 21) & 0b1) == 0b1;
        var sourceSpsr = ((instruction >> 22) & 0b1) == 0b1;

        var sourceStr = sourceSpsr
            ? "spsr_" + core.Cpsr.Mode.ToString() 
            : "cpsr";

        if (isMsr)
        {
            if (((instruction >> 16) & 0b1) == 0b1)
            {
                sourceStr += "flgs";
            }

            if (((instruction >> 25) & 0b1) == 0b1)
            {
                var imm = instruction & 0xFF;
                var rot = (instruction >> 8) & 0b1111;
                var val = Shifter.RORNoFlagsIncRRX(imm, (byte)rot, ref core.Cpsr);

                return $"MSR {sourceStr}, #{val:X8}";
            }
            else
            {
                var rm = instruction & 0b1111;
                return $"MSR {sourceStr}, {RString(rm)}={core.R[rm]:X8}";
            }
        }
        else
        {
            var rd = (instruction >> 12) & 0b1111;
            return $"MRS {RString(rd)}, {sourceStr}";
        }
    }

    private static string DisassembleArmBlockDataTransfer(Core _core, uint instruction)
    {
        var pre = ((instruction >> 24) & 0b1) == 0b1;
        var up = ((instruction >> 23) & 0b1) == 0b1;
        var s = ((instruction >> 22) & 0b1) == 0b1; // TODO - Not used atm
        var writeback = ((instruction >> 21) & 0b1) == 0b1 ? "!" : "";
        var load = ((instruction >> 20) & 0b1) == 0b1;
        var baseReg = ((instruction >> 16) & 0b1111);

        var baseStr = load ? "LDM" : "STM";
        baseStr += (up, pre) switch
        {
            (true, true) => "IB",
            (true, false) => "IA",
            (false, true) => "DB",
            (false, false) => "DA"
        };

        var regString = "";
        for (var ii = 0; ii < 15; ii++)
        {
            if (((instruction >> ii) & 0b1) == 0b1)
            {
                regString += $"r{ii}"; // TODO - Better assembly here will use ranges
            }
        }

        return $"{baseStr}{{0}} {RString(baseReg)}{writeback}, {{{{{regString}}}}}";
    }

    private static string DisassembleArmSingleDataTransfer(Core core, uint instruction)
    {
        var i = ((instruction >> 25) & 0b1) == 0b1;
        var p = ((instruction >> 24) & 0b1) == 0b1;
        var u = ((instruction >> 23) & 0b1) == 0b1;
        var b = ((instruction >> 22) & 0b1) == 0b1;
        var w = ((instruction >> 21) & 0b1) == 0b1;
        var l = ((instruction >> 20) & 0b1) == 0b1;

        return (l) ? "LDR" : "STR";
    }

    private static string DisassembleArmHalfWordImmOffset(Core core, uint instruction)
    {
        var p = ((instruction >> 24) & 0b1) == 0b1;
        var u = ((instruction >> 23) & 0b1) == 0b1;
        var w = ((instruction >> 21) & 0b1) == 0b1;
        var l = ((instruction >> 20) & 0b1) == 0b1;
        var rn = ((instruction >> 16) & 0b1111);
        var rd = ((instruction >> 12) & 0b1111);
        var rm = instruction & 0b1111;
        var sh = ((instruction >> 5) & 0b11);

        if (l) return "LDRH"; 
        else return "STRH"; // TODO - Proper disassembly
    }

    private static string DisassembleArmHalfWordRegOffset(Core core, uint instruction)
    {
        var l = ((instruction >> 20) & 0b1) == 0b1;
        if (l) return "LDRH";
        else return "STRH"; // TODO - Proper disassembly
    }

    private static string DisassembleArmSingleDataSwap(Core core, uint instruction)
    {
        return "SWP"; // TODO
    }

    private static string DisassembleArmMultiplyLong(Core core, uint instruction)
    {
        return "MULL"; // TODO
    }

    private static string DisassembleArmMultiply(Core core, uint instruction)
    {
        return "MUL"; // TODO
    }

    private static string DisassembleArmSwi(Core _core, uint instruction)
    {
        var biosFunctionIx = (instruction & 0xFF_FFFF) >> 16;
        var biosFunctionName = biosFunctionIx switch
        {
            0x00 => "SoftReset",
            0x08 => "Sqrt",
            0x01 => "RegisterRamReset",
            0x09 => "ArcTan",
            0x02 => "Halt",
            0x0A => "ArcTan2",
            0x03 => "Stop",
            0x0B => "CPUSet",
            0x04 => "IntrWait",
            0x0C => "CPUFastSet",
            0x05 => "VBlankIntrWait",
            0x0D => "BiosChecksum",
            0x06 => "Div",
            0x0E => "BgAffineSet",
            0x07 => "DivArm",
            0x0F => "ObjAffineSet",
            0x10 => "BitUnPack",
            0x18 => "Diff16bitUnFilter",
            0x11 => "LZ77UnCompWRAM",
            0x19 => "SoundBiasChange",
            0x12 => "LZ77UnCompVRAM",
            0x1A => "SoundDriverInit",
            0x13 => "HuffUnComp",
            0x1B => "SoundDriverMode",
            0x14 => "RLUnCompWRAM",
            0x1C => "SoundDriverMain",
            0x15 => "RLUnCompVRAM",
            0x1D => "SoundDriverVSync",
            0x16 => "Diff8bitUnFilterWRAM",
            0x1E => "SoundChannelClear",
            0x17 => "Diff8bitUnFilterVRAM",
            0x1F => "MIDIKey2Freq",
            0x20 => "MusicPlayerOpen",
            0x28 => "SoundDriverVSyncOff",
            0x21 => "MusicPlayerStart",
            0x29 => "SoundDriverVSyncOn",
            0x22 => "MusicPlayerStop",
            0x2A => "GetJumpList",
            0x23 => "MusicPlayerContinue",
            0x24 => "MusicPlayerFadeOut",
            0x25 => "MultiBoot",
            0x26 => "HardReset",
            0x27 => "CustomHalt",
            _ => "Invalid Bios function"
        };

        return $"SWI #{biosFunctionIx << 16:X6} = {biosFunctionName}";
    }

    private static string DisassembleArmCoProcessorDataTransfer(Core core, uint instruction)
    {
        throw new NotImplementedException();
    }

    private static string DisassembleArmCoprocessorRegTransfer(Core core, uint instruction) => "Coprocessor Reg Transfer";

    private static string DisassembleArmCoprocessorDataOp(Core core, uint instruction)
    {
        throw new NotImplementedException();
    }

    private static string DisassembleArmDataOp(Core core, uint instruction)
    {
        var imm = ((instruction >> 25) & 0b1) == 1;
        var opcode = ((instruction >> 21) & 0b1111);
        var s = ((instruction >> 20) & 0b1) == 1;
        var rn = (instruction >> 16) & 0b1111;
        var rd = (instruction >> 12) & 0b1111;
        var op2 = instruction & 0b1111_1111_1111;

        var opcodeStr = opcode switch
        {
            0 => "AND",
            1 => "EOR",
            2 => "SUB",
            3 => "RSB",
            4 => "ADD",
            5 => "ADC",
            6 => "SBC",
            7 => "RSC",
            8 => "TST",
            9 => "TEQ",
            10 => "CMP",
            11 => "CMN",
            12 => "ORR",
            13 => "MOV",
            14 => "BIC",
            15 => "MVN",
            _ => throw new Exception(),
        };

        if (s) opcodeStr += "S";

        string operand2;

        if (imm)
        {
            var i = op2 & 0b1111_1111;
            var r = (op2 >> 8) & 0b1111;
            operand2 = $"#{Shifter.RORNoFlagsIncRRX(i, (byte)(r * 2), ref core.Cpsr):X}";
        }
        else
        {
            var rm = op2 & 0b1111;
            var useReg = ((op2 >> 4) & 0b1) == 1;
            var shiftType = ((op2 >> 5) & 0b11) switch
            {
                0b00 => "LSL",
                0b01 => "LSR",
                0b10 => "ASR",
                0b11 => "ROR",
                _ => throw new Exception()
            };

            if (useReg)
            {
                var rs = (op2 >> 8) & 0b1111;
                operand2 = $"{RString(rm)},{shiftType} {RString(rs)}";
            }
            else
            {
                var amount = (op2 >> 7) & 0b1_1111;
                operand2 = $"{RString(rm)},{shiftType} #{amount:X}";
            }
        }

        return opcodeStr switch
        {
            var _ when opcodeStr[..3] is "TST" or "TEQ" or "CMP" or "CMN" => $"{opcodeStr[..3]}{{0}} {RString(rn)},{operand2}",
            var _ when opcodeStr is "MOV" or "MVN" => $"{opcodeStr}{{0}} {RString(rd)},{operand2}",
            _ => $"{opcodeStr}{{0}} {RString(rd)},{RString(rn)},{operand2}",
        };
    }

    private static string DisassembleArmBranch(Core core, uint instruction)
    {
        var name = ((instruction >> 24) & 0b1) == 0b1 ? "BL{0}" : "B{0}";

        var offset = ((int)((instruction & 0xFF_FFFF) << 8)) >> 6;

        return $"{name} #{core.R[15] + offset:X8}";
    }

    private static string DisassembleArmBranchEx(Core core, uint instruction)
    {
        var rn = instruction & 0b1111;
        return $"BX{{0}} {RString(rn)}={core.R[rn]:X8}";
    }
}
