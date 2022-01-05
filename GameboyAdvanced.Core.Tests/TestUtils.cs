using System;
using Xunit;

namespace GameboyAdvanced.Core.Tests;

public class TestUtils
{
    [Fact]
    public void TestReadWriteHalfWord()
    {
        var b = new byte[0x100];
        new Random().NextBytes(b);

        Utils.WriteHalfWord(b, 0xFF, 0, 0x1122);
        Assert.Equal(0x22, b[0]);
        Assert.Equal(0x11, b[1]);
        Assert.Equal(0x1122, Utils.ReadHalfWord(b, 0, 0xFF));
    }

    [Fact]
    public void TestReadWriteHalfWordUnaligned()
    {
        var b = new byte[0x100];
        new Random().NextBytes(b);

        Utils.WriteHalfWord(b, 0xFF, 1, 0x1122);
        Assert.Equal(0x22, b[1]);
        Assert.Equal(0x11, b[2]);
        Assert.Equal(0x1122, Utils.ReadHalfWord(b, 1, 0xFF));
    }

    [Fact]
    public void TestReadWriteHalfWordWrap()
    {
        var b = new byte[0x100];
        new Random().NextBytes(b);

        Utils.WriteHalfWord(b, 0xFF, 0xFF, 0x1122);
        Assert.Equal(0x22, b[0xFF]);
        Assert.Equal(0x11, b[0x00]);
        Assert.Equal(0x1122, Utils.ReadHalfWord(b, 0xFF, 0xFF));
    }

    [Fact]
    public void TestReadWriteWord()
    {
        var b = new byte[0x100];
        new Random().NextBytes(b);

        Utils.WriteWord(b, 0xFF, 0, 0x11223344);
        Assert.Equal(0x44, b[0]);
        Assert.Equal(0x33, b[1]);
        Assert.Equal(0x22, b[2]);
        Assert.Equal(0x11, b[3]);
        Assert.Equal((uint)0x11223344, Utils.ReadWord(b, 0, 0xFF));
    }

    [Fact]
    public void TestReadWriteWordUnaligned()
    {
        var b = new byte[0x100];
        new Random().NextBytes(b);

        Utils.WriteWord(b, 0xFF, 1, 0x11223344);
        Assert.Equal(0x44, b[1]);
        Assert.Equal(0x33, b[2]);
        Assert.Equal(0x22, b[3]);
        Assert.Equal(0x11, b[4]);
        Assert.Equal((uint)0x11223344, Utils.ReadWord(b, 1, 0xFF));
    }

    [Fact]
    public void TestReadWriteWordWrap()
    {
        var b = new byte[0x100];
        new Random().NextBytes(b);

        Utils.WriteWord(b, 0xFF, 0xFE, 0x11223344);
        Assert.Equal(0x44, b[0xFE]);
        Assert.Equal(0x33, b[0xFF]);
        Assert.Equal(0x22, b[0x00]);
        Assert.Equal(0x11, b[0x01]);
        Assert.Equal((uint)0x11223344, Utils.ReadWord(b, 0xFE, 0xFF));
    }
}
