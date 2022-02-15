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
            var i when ((i & 0b1111_1111) == 0b1101_1111) => "SWI",
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
            var i when ((i & 0b1111_1100) == 0b0100_0100) => DisassemblyHiRegBx(core, instruction),
            var i when ((i & 0b1111_1100) == 0b0100_0000) => DiassembleAlu(core, instruction),
            var i when ((i & 0b1110_0000) == 0b0010_0000) => "Move/compare/add/sub #imm",
            var i when ((i & 0b1111_1000) == 0b0001_1000) => "ADD/SUB",
            var i when ((i & 0b1110_0000) == 0b0000_0000) => "MOV Shifted reg",
            _ => throw new Exception("Invalid thumb instruction"),
        };
    }

    private static string DiassembleAlu(Core core, uint instruction)
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

    private static string DisassemblyHiRegBx(Core core, uint instruction)
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
}
