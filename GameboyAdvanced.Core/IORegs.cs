using System.Reflection;

namespace GameboyAdvanced.Core;

public static class IORegs
{
    public const uint DISPCNT = 0x04000000;
    public const uint GREENSWAP = 0x04000002;
    public const uint DISPSTAT = 0x04000004;
    public const uint VCOUNT = 0x04000006;
    public const uint BG0CNT = 0x04000008;
    public const uint BG1CNT = 0x0400000A;
    public const uint BG2CNT = 0x0400000C;
    public const uint BG3CNT = 0x0400000E;
    public const uint BG0HOFS = 0x04000010;
    public const uint BG0VOFS = 0x04000012;
    public const uint BG1HOFS = 0x04000014;
    public const uint BG1VOFS = 0x04000016;
    public const uint BG2HOFS = 0x04000018;
    public const uint BG2VOFS = 0x0400001A;
    public const uint BG3HOFS = 0x0400001C;
    public const uint BG3VOFS = 0x0400001E;
    public const uint BG2PA = 0x04000020;
    public const uint BG2PB = 0x04000022;
    public const uint BG2PC = 0x04000024;
    public const uint BG2PD = 0x04000026;
    public const uint BG2X_L = 0x04000028;
    public const uint BG2X_H = 0x0400002A;
    public const uint BG2Y_L = 0x0400002C;
    public const uint BG2Y_H = 0x0400002E;
    public const uint BG3PA = 0x04000030;
    public const uint BG3PB = 0x04000032;
    public const uint BG3PC = 0x04000034;
    public const uint BG3PD = 0x04000036;
    public const uint BG3X_L = 0x04000038;
    public const uint BG3X_H = 0x0400003A;
    public const uint BG3Y_L = 0x0400003C;
    public const uint BG3Y_H = 0x0400003E;
    public const uint WIN0H = 0x04000040;
    public const uint WIN1H = 0x04000042;
    public const uint WIN0V = 0x04000044;
    public const uint WIN1V = 0x04000046;
    public const uint WININ = 0x04000048;
    public const uint WINOUT = 0x0400004A;
    public const uint MOSAIC = 0x0400004C;
    public const uint BLDCNT = 0x04000050;
    public const uint BLDALPHA = 0x04000052;
    public const uint BLDY = 0x04000054;
    public const uint SOUND1CNT_L = 0x04000060;
    public const uint SOUND1CNT_H = 0x04000062;
    public const uint SOUND1CNT_X = 0x04000064;
    public const uint SOUND2CNT_L = 0x04000068;
    public const uint SOUND2CNT_H = 0x0400006C;
    public const uint SOUND3CNT_L = 0x04000070;
    public const uint SOUND3CNT_H = 0x04000072;
    public const uint SOUND3CNT_X = 0x04000074;
    public const uint SOUND4CNT_L = 0x04000078;
    public const uint SOUND4CNT_H = 0x0400007C;
    public const uint SOUNDCNT_L = 0x04000080;
    public const uint SOUNDCNT_H = 0x04000082;
    public const uint SOUNDCNT_X = 0x04000084;
    public const uint SOUNDBIAS = 0x04000088;
    public const uint FIFO_A = 0x040000A0;
    public const uint FIFO_B = 0x040000A4;
    public const uint DMA0SAD = 0x040000B0;
    public const uint DMA0DAD = 0x040000B4;
    public const uint DMA0CNT_L = 0x040000B8;
    public const uint DMA0CNT_H = 0x040000BA;
    public const uint DMA1SAD = 0x040000BC;
    public const uint DMA1DAD = 0x040000C0;
    public const uint DMA1CNT_L = 0x040000C4;
    public const uint DMA1CNT_H = 0x040000C6;
    public const uint DMA2SAD = 0x040000C8;
    public const uint DMA2DAD = 0x040000CC;
    public const uint DMA2CNT_L = 0x040000D0;
    public const uint DMA2CNT_H = 0x040000D2;
    public const uint DMA3SAD = 0x040000D4;
    public const uint DMA3DAD = 0x040000D8;
    public const uint DMA3CNT_L = 0x040000DC;
    public const uint DMA3CNT_H = 0x040000DE;
    public const uint TM0CNT_L = 0x04000100;
    public const uint TM0CNT_H = 0x04000102;
    public const uint TM1CNT_L = 0x04000104;
    public const uint TM1CNT_H = 0x04000106;
    public const uint TM2CNT_L = 0x04000108;
    public const uint TM2CNT_H = 0x0400010A;
    public const uint TM3CNT_L = 0x0400010C;
    public const uint TM3CNT_H = 0x0400010E;
    public const uint SIODATA32 = 0x04000120;
//    public const uint SIOMULTI0 = 0x04000120;
    public const uint SIOMULTI1 = 0x04000122;
    public const uint SIOMULTI2 = 0x04000124;
    public const uint SIOMULTI3 = 0x04000126;
    public const uint SIOCNT = 0x04000128;
//    public const uint SIOMLT_SEND = 0x0400012A;
    public const uint SIODATA8 = 0x0400012A;
    public const uint KEYINPUT = 0x04000130;
    public const uint KEYCNT = 0x04000132;
    public const uint RCNT = 0x04000134;
    public const uint IR = 0x04000136; // Ancient - Infrared Register (Prototypes only)
    public const uint JOYCNT = 0x04000140;
    public const uint JOY_RECV = 0x04000150;
    public const uint JOY_TRANS = 0x04000154;
    public const uint JOYSTAT = 0x04000158;
    public const uint IE = 0x04000200;
    public const uint IF = 0x04000202;
    public const uint WAITCNT = 0x04000204;
    public const uint IME = 0x04000208;
    public const uint POSTFLG = 0x04000300;
    public const uint HALTCNT = 0x04000301;
    public const uint UNDOCUMENTED_410 = 0x04000410;
    public const uint INTMEMCTRL = 0x04000800;

    private readonly static Dictionary<uint, string> _cachedNameMapping = new();

    /// <summary>
    /// Used to provide better names for addresses whilst debugging.
    /// </summary>
    public static string? GetNameFromAddress(uint address)
    {
        if (!_cachedNameMapping.Any())
        {
            var constants = typeof(IORegs)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral);
            foreach (var constant in constants)
            {
                _cachedNameMapping.Add((uint)constant.GetRawConstantValue()!, constant.Name);
            }
        }

        return _cachedNameMapping.TryGetValue(address, out var name) ? name : null;
    }
}
