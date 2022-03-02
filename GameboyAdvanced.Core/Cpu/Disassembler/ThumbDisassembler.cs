using static GameboyAdvanced.Core.Cpu.Disassembler.Utils;
namespace GameboyAdvanced.Core.Cpu.Disassembler;

internal static class ThumbDisassembler
{
    internal static string Disassemble(Core core, ushort instruction)
    {
        // TODO - Improve disassembly from this mess
        return (instruction >> 8) switch
        {
            var i when ((i & 0b1111_0000) == 0b1111_0000) => "Long branch with link",
            var i when ((i & 0b1111_1000) == 0b1110_0000) => "Unconditional branch",
            var i when ((i & 0b1111_1111) == 0b1101_1111) => DisassembleSwi(core, instruction),
            var i when ((i & 0b1111_0000) == 0b1101_0000) => "Conditional branch",
            var i when ((i & 0b1111_0000) == 0b1100_0000) => "Multiple load/store",
            var i when ((i & 0b1111_0110) == 0b1011_0100) => "PUSH/POP registers",
            var i when ((i & 0b1111_1111) == 0b1011_0000) => "Add offset to SP",
            var i when ((i & 0b1111_0000) == 0b1010_0000) => "Load address",
            var i when ((i & 0b1111_0000) == 0b1001_0000) => "SP-relative load store",
            var i when ((i & 0b1111_0000) == 0b1000_0000) => "LDRH/STRH",
            var i when ((i & 0b1110_0000) == 0b0110_0000) => "LDR #imm/STR #imm",
            var i when ((i & 0b1111_0010) == 0b0101_0010) => "LDR/STR sign extended",
            var i when ((i & 0b1111_0010) == 0b0101_0000) => "LDR/STR reg offset",
            var i when ((i & 0b1111_1000) == 0b0100_1000) => "PC-relative load",
            var i when ((i & 0b1111_1100) == 0b0100_0100) => DisassembleHiRegBx(core, instruction),
            var i when ((i & 0b1111_1100) == 0b0100_0000) => DisassembleAlu(core, instruction),
            var i when ((i & 0b1110_0000) == 0b0010_0000) => "Move/compare/add/sub #imm",
            var i when ((i & 0b1111_1000) == 0b0001_1000) => "ADD/SUB",
            var i when ((i & 0b1110_0000) == 0b0000_0000) => "MOV Shifted reg",
            _ => "Invalid thumb instruction",
        };
    }

    private static string DisassembleAlu(Core core, uint instruction)
    {
        var opcode = (instruction >> 6) & 0b1111;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        return opcode switch
        {
            0x0 => $"AND {RString(rd)}, {RString(rs)}",
            0x1 => $"EOR {RString(rd)}, {RString(rs)}",
            0x2 => $"LSL {RString(rd)}, {RString(rs)}",
            0x3 => $"LSR {RString(rd)}, {RString(rs)}",
            0x4 => $"ASR {RString(rd)}, {RString(rs)}",
            0x5 => $"ADC {RString(rd)}, {RString(rs)}",
            0x6 => $"SBC {RString(rd)}, {RString(rs)}",
            0x7 => $"ROR {RString(rd)}, {RString(rs)}",
            0x8 => $"TST {RString(rd)}, {RString(rs)}",
            0x9 => $"NEG {RString(rd)}, {RString(rs)}",
            0xA => $"CMP {RString(rd)}, {RString(rs)}",
            0xB => $"CMN {RString(rd)}, {RString(rs)}",
            0xC => $"ORR {RString(rd)}, {RString(rs)}",
            0xD => $"MUL {RString(rd)}, {RString(rs)}",
            0xE => $"BIC {RString(rd)}, {RString(rs)}",
            0xF => $"MVN {RString(rd)}, {RString(rs)}",
            _ => throw new Exception($"Invalid opcode for ALU thumb {opcode:X2}")
        };
    }

    private static string DisassembleHiRegBx(Core core, uint instruction)
    {
        var opcode = (instruction >> 8) & 0b11;
        var msbd = (instruction >> 7) & 0b1;
        var msbs = (instruction >> 6) & 0b1;
        var rs = (instruction >> 3) & 0b111;
        var rd = instruction & 0b111;

        var fullRd = rd | (msbd << 3);
        var fullRs = rs | (msbs << 3);

        var operand = core.R[fullRs];

        return opcode switch
        {
            0b00 => $"ADD {RString(fullRd)}, {RString(fullRs)}",
            0b01 => $"CMP {RString(fullRd)}, {RString(fullRs)}",
            0b10 => $"MOV {RString(fullRd)}, {RString(fullRs)}",
            0b11 => $"BX {RString(fullRs)}",
            _ => throw new Exception($"Invalid opcode for hi reg thumb op {opcode:X2}")
        };
    }

    internal static string DisassembleSwi(Core core, uint instruction)
    {
        var biosFunctionIx = instruction & 0xFF;
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

        return $"SWI #{biosFunctionIx:X2} = {biosFunctionName}";
    }
}
