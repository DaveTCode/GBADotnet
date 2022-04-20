using GameboyAdvanced.Core;
using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Rom;
using System;
using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace GameboyAdvanced.Rom.Tests;

public class BiosFixture : IDisposable
{
    public readonly static string BiosPath = Path.Join("..", "..", "..", "..", "roms", "real", "gba_bios.bin");
    public readonly byte[] Bios;
    private bool disposedValue;

    public BiosFixture()
    {
        Bios = File.ReadAllBytes(BiosPath);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

public class RomTests : IClassFixture<BiosFixture>
{
    private readonly byte[] _bios;

    public RomTests(BiosFixture biosFixture)
    {
        _bios = biosFixture.Bios;
    }

    [Theory]
    [InlineData(@"..\..\..\..\roms\test\panda.gba", "4FB353DB93F8ECD2A65F17368831EFAA", 20)]
    [InlineData(@"..\..\..\..\roms\test\beeg.gba", "23A253D08FD69B77AEE0C98F58B332D7", 17)]
    [InlineData(@"..\..\..\..\roms\test\tonc\first.gba", "BCB71B5B5F0EC5806BF90281353814D6", 20)]
    [InlineData(@"..\..\..\..\roms\test\tonc\hello.gba", "B9E014043B3B7FF125F5E8D6BCC8DFC2", 26)]
    [InlineData(@"..\..\..\..\roms\test\tonc\irq_demo.gba", "85883617F354E4F6F1E5B1D1B38F0472", 33)]
    [InlineData(@"..\..\..\..\roms\test\tonc\bigmap.gba", "232EDF4D91A951545B92B49900A8FDCC", 39)]
    [InlineData(@"..\..\..\..\roms\test\tonc\bld_demo.gba", "F0D05565B6F6C358F6FBFAABB03079F5", 27)]
    [InlineData(@"..\..\..\..\roms\test\tonc\bm_modes.gba", "02706CBEF3C2D53C2026043447FFEAEE", 25)]
    [InlineData(@"..\..\..\..\roms\test\tonc\brin_demo.gba", "F564F81A77A78579E5EA581E45EBD026", 27)]
    [InlineData(@"..\..\..\..\roms\test\tonc\cbb_demo.gba", "BC380690FD79A42F179B28705F6DA675", 19)]
    [InlineData(@"..\..\..\..\roms\test\tonc\dma_demo.gba", "EB332BCDBDBF341206A92DFA8B7641A2", 25)]
    [InlineData(@"..\..\..\..\roms\test\tonc\m3_demo.gba", "1FB3E64635696FBE782C7930B071B784", 27)]
    [InlineData(@"..\..\..\..\roms\test\tonc\m7_demo.gba", "280266487D49A96FF936AA5AB58915D6", 24)]
    [InlineData(@"..\..\..\..\roms\test\tonc\m7_demo_mb.gba", "280266487D49A96FF936AA5AB58915D6", 24)]
    [InlineData(@"..\..\..\..\roms\test\tonc\m7_ex.gba", "FA64F20EA7DFFAB2B38FC7EA78532D63", 27)]
    [InlineData(@"..\..\..\..\roms\test\tonc\oacombo.gba", "7DB1572DF445A33FC86446DFAB127712", 19)]
    [InlineData(@"..\..\..\..\roms\test\tonc\obj_demo.gba", "89E13BD254C6ED8005AB8E15F7E4C00E", 34)]
    [InlineData(@"..\..\..\..\roms\test\tonc\obj_aff.gba", "4D2FCE5DB90F2D023F012200EDF3EDFA", 27)]
    [InlineData(@"..\..\..\..\roms\test\tonc\octtest.gba", "9889646F439671EB682F2FEE6C7083C5", 20)]
    [InlineData(@"..\..\..\..\roms\test\tonc\prio_demo.gba", "65D16C6D651CE66E6E2E2179BBF180B1", 25)]
    [InlineData(@"..\..\..\..\roms\test\tonc\sbb_aff.gba", "6608E892EA60FAADE96D97064D90A7E4", 23)]
    [InlineData(@"..\..\..\..\roms\test\tonc\sbb_reg.gba", "F9CAE317D7CC6B4ABB9AE3AACE9297C7", 29)]
    [InlineData(@"..\..\..\..\roms\test\tonc\second.gba", "BCB71B5B5F0EC5806BF90281353814D6", 20)]
    [InlineData(@"..\..\..\..\roms\test\tonc\swi_demo.gba", "8B7149DBC3B5612363223DEA1064BD13", 26)]
    [InlineData(@"..\..\..\..\roms\test\tonc\swi_vsync.gba", "E1C570A1A2E5D82A8FAE557F9F4C3E49", 26)]
    [InlineData(@"..\..\..\..\roms\test\tonc\tmr_demo.gba", "DBE6B2C7903A1E85FF962F557F68BD8C", 28)]
    [InlineData(@"..\..\..\..\roms\test\tonc\tte_demo.gba", "7901DCCA254F65E044C4B22624DAE46C", 29)]
    [InlineData(@"..\..\..\..\roms\test\tonc\txt_obj.gba", "792C69B652B0A6F174DF7DFDABF5E180", 57)]
    [InlineData(@"..\..\..\..\roms\test\tonc\txt_se1.gba", "236C416A76F83CB6913425AF8EADAB95", 39)]
    [InlineData(@"..\..\..\..\roms\test\tonc\txt_se2.gba", "EB37546021251F4125073DFA16845C83", 35)]
    [InlineData(@"..\..\..\..\roms\test\tonc\win_demo.gba", "AF54C8D8336728A1082E14C1A2DA1FDE", 19)]
    [InlineData(@"..\..\..\..\roms\test\cpu_test\CPUTest.gba", "6298DFCB1E6057D7E8DE42C9678557AD", 27)]
    [InlineData(@"..\..\..\..\roms\test\DenSinH\THUMB_Any.gba", "339DDC3899B1FB235E0FADE4150CE1FF", 846)]
    [InlineData(@"..\..\..\..\roms\test\DenSinH\ARM_Any.gba", "339DDC3899B1FB235E0FADE4150CE1FF", 842)]
    [InlineData(@"..\..\..\..\roms\test\DenSinH\eeprom-test\main.gba", "5C52E793235D7D4C0D57E090941561D8", 72)]
    [InlineData(@"..\..\..\..\roms\test\DenSinH\flash-test\main.gba", "5C52E793235D7D4C0D57E090941561D8", 204)]
    [InlineData(@"..\..\..\..\roms\test\DenSinH\mandelbrot\main.gba", "DAC14635474AE370C91CA367E82249A4", 40)]
    [InlineData(@"..\..\..\..\roms\test\jsmolka\arm\arm.gba", "B2874BF21EB2362FB122BF974B169CF5", 28)]
    [InlineData(@"..\..\..\..\roms\test\jsmolka\thumb\thumb.gba", "B2874BF21EB2362FB122BF974B169CF5", 23)]
    [InlineData(@"..\..\..\..\roms\test\jsmolka\memory\memory.gba", "B2874BF21EB2362FB122BF974B169CF5", 25)]
    [InlineData(@"..\..\..\..\roms\test\jsmolka\nes\nes.gba", "B2874BF21EB2362FB122BF974B169CF5", 18)]
    [InlineData(@"..\..\..\..\roms\test\jsmolka\ppu\hello.gba", "F7C6C22B215F45C6832302B4BA9A09F2", 23)]
    [InlineData(@"..\..\..\..\roms\test\jsmolka\ppu\shades.gba", "8D13296557FF246E0E1916647BCB1EFE", 22)]
    [InlineData(@"..\..\..\..\roms\test\jsmolka\ppu\stripes.gba", "09C9C8AED12CD0C3DEE87FC8B31DEBE9", 19)]
    [InlineData(@"..\..\..\..\roms\test\jsmolka\bios\bios.gba", "B2874BF21EB2362FB122BF974B169CF5", 18)]
    [InlineData(@"..\..\..\..\roms\test\jsmolka\save\none.gba", "B2874BF21EB2362FB122BF974B169CF5", 21)]
    [InlineData(@"..\..\..\..\roms\test\jsmolka\save\sram.gba", "B2874BF21EB2362FB122BF974B169CF5", 21)]
    [InlineData(@"..\..\..\..\roms\test\jsmolka\save\flash64.gba", "B2874BF21EB2362FB122BF974B169CF5", 102)]
    [InlineData(@"..\..\..\..\roms\test\jsmolka\save\flash128.gba", "B2874BF21EB2362FB122BF974B169CF5", 90)]
    [InlineData(@"..\..\..\..\roms\test\jsmolka\unsafe\unsafe.gba", "B2874BF21EB2362FB122BF974B169CF5", 27)]
    [InlineData(@"..\..\..\..\roms\test\nba\cpu\irqdelay\irqdelay.gba", "D26D24D84949983044A17075D9A77662", 14)]
    [InlineData(@"..\..\..\..\roms\test\nba\dma\latch\latch.gba", "4DF7BF7C2F11FB00DF18E477B23E207C", 25)]
    [InlineData(@"..\..\..\..\roms\test\nba\dma\start-delay\start-delay.gba", "C8FEFEBBEA0EA84B32DD8D6BC7E2305B", 26)]
    [InlineData(@"..\..\..\..\roms\test\nba\haltcnt\haltcnt.gba", "0F5B57883BB0F9BF9C3B414F35E045ED", 20)]
    [InlineData(@"..\..\..\..\roms\test\nba\ppu\basic-timing\basic-timing.gba", "B02D6DD5816ED742229C205CBBC2B0C7", 23)]
    [InlineData(@"..\..\..\..\roms\test\nba\timer\start-stop\start-stop.gba", "5112494B85407AF7430AA6E96FC6310D", 19)]
    [InlineData(@"..\..\..\..\roms\test\nba\timer\reload\reload.gba", "98C13B548AC6CD73897BA207D1F50139", 20)]
    [InlineData(@"..\..\..\..\roms\test\nba\dma\burst-into-tears\burst-into-tears.gba", "9FFC54316445124C6B914BF84890D1E1", 20)]
    [InlineData(@"..\..\..\..\roms\test\retAddr.gba", "1F98A902677886658132DD28FFE784D4", 24)]
    [InlineData(@"..\..\..\..\roms\test\openbus\openbus_bios_misaligned.gba", "25B7E300274EA913101AACDEAA65A920", 22)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\3DEngine\3DEngine.gba", "061B6A104F5CF724D25604FA294BC9E5", 22)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\HelloWorld\HelloWorld.gba", "6732D771BA70118A6F666B9567D45A5E", 6)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\LCD\BG\BGMode0\BGMode0.gba", "A895B8D7F1C67379C834970A0CA9CA9A", 19)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\LCD\BG\BGMode7\BGMode7.gba", "EDBEEFEF70EC44F605505F5C1B7D1F30", 17)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\LCD\BG\BGRotZoomMode2\BGRotZoomMode2.gba", "7880492E1BA1C28E3C3609F576FEA945", 20)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\LCD\BG\BGRotZoomMode3\BGRotZoomMode3.gba", "D0C0CD6814822720C0D3BDA4CAAADDDC", 15)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\LCD\BG\BGRotZoomMode4\BGRotZoomMode4.gba", "99E075F603D4B72A6CEE9275D3D91C9F", 14)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\LCD\BG\BGRotZoomMode5\BGRotZoomMode5.gba", "226BD30D95AF3047F4A797F873E519B3", 17)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\LCD\OBJ\OBJRotZoom4BPP\OBJRotZoom4BPP.gba", "394E40E65796FF384CEBA1E1DA13FCD5", 14)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\LCD\OBJ\OBJRotZoom8BPP\OBJRotZoom8BPP.gba", "394E40E65796FF384CEBA1E1DA13FCD5", 16)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\FastLine\FastLine.gba", "0D0EAD478E07A952CA1B49C74C418C84", 21)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\FastLineClip\FastLineClip.gba", "0D0EAD478E07A952CA1B49C74C418C84", 19)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\CylinderMap\CylinderMap.gba", "B9D4EC6A323435EE0B50DE3A871A2B6D", 20)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\Myst\Myst.gba", "8557EA66AA223E9AC0F86BCAF18B2AA9", 119)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\Video\BigBuckBunny\BigBuckBunny.gba", "82CF3A1A26586961BF11950B84B1755F", 17)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\Timers\Timers.gba", "516C6B0DFB5D5831AE80628BCDEE0877", 10)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Arithmetic\ARCTAN\BIOSARCTAN.gba", "EDA6E62543DA5DF529DEF0C6C26F7365", 12)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Arithmetic\DIV\BIOSDIV.gba", "818416AB6660A7139C2CF6E831C10428", 22)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Arithmetic\SQRT\BIOSSQRT.gba", "73B25397440F8A4D7B8D9F77EC4A6B49", 11)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Decompress\BIT\1BPP\BIOSBIT1BPP.gba", "C72C2FEACC2DAD7D50C76CEE1EBD10E0", 14)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Decompress\BIT\2BPP\BIOSBIT2BPP.gba", "FC660CF25F1C59C96E99A966A63169CE", 10)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Decompress\BIT\4BPP\BIOSBIT4BPP.gba", "60153E3A7D12ADAE46E786815C1B4970", 19)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Decompress\BIT\8BPP\BIOSBIT8BPP.gba", "1EF843D4DA1F49436FD8BB96D8D7EDF7", 20)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Decompress\DIFF\BIOSDIFF.gba", "DBF96D9B4D2D49E41807B459AD74B268", 17)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Decompress\HUFFMAN\BIOSHUFFMAN.gba", "AECB6709F374B3EF5BACDB15C575B204", 15)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Decompress\LZ77\BIOSLZ77.gba", "C96D92A2B831128CCAEBA6EADAC8BC9F", 32)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Decompress\RLE\BIOSRLE.gba", "9647F5512EFAC9EA43CB564FA93F9FB1", 21)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\MemoryCopy\CPUFASTSET\BIOSCPUFASTSET.gba", "635D8584EC7AD34B3B1F2A58EB178A0D", 16)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\MemoryCopy\CPUSET\BIOSCPUSET.gba", "1CEAD461012CF751A889811D15439A0D", 22)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Misc\CHECKSUM\BIOSCHECKSUM.gba", "AD9BC828CFD883CEF4537F6667065D3D", 24)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Reset\RegisterRam\BIOSRegisterRamReset.gba", "7BDFC2AAB74607750BA861B6BC6CECB7", 8)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\RotationScaling\BGAffineSet\BIOSBGAFFINESET.gba", "1F6E203B7F7B693BFE9E04FE4D7C8A5B", 24)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\RotationScaling\OBJAffineSet\BIOSOBJAFFINESET.gba", "CC63FD2EA0ADEE03EC5C66E69060FDF3", 17)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Sound\Bias\BIOSSoundBias.gba", "9986A53128D00DBDBF4797C534FFA580", 20)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Sound\ChannelClear\BIOSSoundChannelClear.gba", "398D176FBFD9D15D676E21D2B8E1A257", 29)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Sound\DriverInit\BIOSSoundDriverInit.gba", "CB071526F294C9B05F2BD207FD04C358", 13)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Sound\DriverMain\BIOSSoundDriverMain.gba", "828759F9560573B59B91F6CF31A179F0", 22)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Sound\DriverMode\BIOSSoundDriverMode.gba", "3AC2F9AFD35F5A1175728DFDE87F334D", 19)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Sound\DriverVSync\BIOSSoundDriverVSync.gba", "C7128354D73A103C0AF64D169438CD92", 11)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Sound\GetJumpList\BIOSSoundGetJumpList.gba", "EBBC6061C8D505B5D05E2213B2D72FD3", 22)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\BIOS\Sound\MidiKey2Freq\BIOSMidiKey2Freq.gba", "A51545F574B9548FB2CB54703872AAB0", 19)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\Physics\2D\Acceleration\Acceleration.gba", "E19355782C9F89DA8ABA2F5A629D21BA", 29)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\Physics\2D\Gravity\Gravity.gba", "A4D70DDFD0BE3C0C0F42764F82F2FC92", 12)]
    [InlineData(@"..\..\..\..\roms\test\PeterLemon\Physics\2D\Velocity\Velocity.gba", "104AA1C171A3CC2B39063BCD3C4E9130", 16)]
    public void TestRom(string romPath, string md5ExpectedString, ulong frames)
    {
        var romPathCanonical = Path.Join(romPath.Split(@"\"));
        var romBytes = File.ReadAllBytes(romPathCanonical);
        var rom = new GamePak(romBytes);
        var device = new Device(_bios, rom, new TestDebugger(), true);

        for (var ii = 0u; ii < frames; ii++)
        {
            device.RunFrame();
        }

        var frame = device.GetFrame();
        using MD5 md5 = MD5.Create();
        var md5Frame = md5.ComputeHash(frame);

        Assert.Equal(md5ExpectedString, Convert.ToHexString(md5Frame));
    }
}
