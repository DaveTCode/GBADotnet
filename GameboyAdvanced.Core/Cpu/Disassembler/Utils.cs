namespace GameboyAdvanced.Core.Cpu.Disassembler;

internal static class Utils
{
    internal static string RString(uint rd) => rd switch
    {
        15 => "PC",
        14 => "LR",
        13 => "SP",
        _ => $"R{rd}"
    };
}
