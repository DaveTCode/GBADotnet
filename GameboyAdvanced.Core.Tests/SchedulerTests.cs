using GameboyAdvanced.Core.Debug;
using GameboyAdvanced.Core.Rom;
using Xunit;

namespace GameboyAdvanced.Core.Tests;

public unsafe class SchedulerTests
{
    private readonly static byte[] _bios = new byte[0x4000];
    private readonly static GamePak _testGamePak = new(new byte[0xFF_FFFF]);

    [Fact]
    public void TestStepEmptyScheduler()
    {
        var device = new Device(_bios, _testGamePak, new TestDebugger(), true);

        var scheduler = new Scheduler(device);

        scheduler.Step();

        Assert.True(true);
    }

    internal static void TestEventR4_1(Device device)
    {
        device.Cpu.R[4] = 1;
    }

    internal static void TestEventR4_2(Device device)
    {
        device.Cpu.R[4] = 2;
    }

    internal static void TestEventR3_1(Device device)
    {
        device.Cpu.R[3] = 1;
    }

    [Fact]
    public void TestAddEventToScheduler()
    {
        var device = new Device(_bios, _testGamePak, new TestDebugger(), true);

        var scheduler = new Scheduler(device);

        scheduler.ScheduleEvent(EventType.Generic, &TestEventR4_1, 2);
        Assert.Equal(0u, device.Cpu.R[4]);
        device.Cpu.Cycles++;
        scheduler.Step();
        Assert.Equal(0u, device.Cpu.R[4]);
        device.Cpu.Cycles++;
        scheduler.Step();
        Assert.Equal(1u, device.Cpu.R[4]);
    }

    [Fact]
    public void TestAddTwoEventsToScheduler()
    {
        var device = new Device(_bios, _testGamePak, new TestDebugger(), true);

        var scheduler = new Scheduler(device);

        scheduler.ScheduleEvent(EventType.Generic, &TestEventR4_1, 2);
        scheduler.ScheduleEvent(EventType.Generic, &TestEventR4_2, 1);
        Assert.Equal(0u, device.Cpu.R[4]);
        device.Cpu.Cycles++;
        scheduler.Step();
        Assert.Equal(2u, device.Cpu.R[4]);
        device.Cpu.Cycles++;
        scheduler.Step();
        Assert.Equal(1u, device.Cpu.R[4]);
    }

    [Fact]
    public void TestAddEventsOnSameCycle()
    {
        var device = new Device(_bios, _testGamePak, new TestDebugger(), true);

        var scheduler = new Scheduler(device);

        scheduler.ScheduleEvent(EventType.Generic, &TestEventR4_2, 1);
        scheduler.ScheduleEvent(EventType.Generic, &TestEventR3_1, 1);
        Assert.Equal(0u, device.Cpu.R[3]);
        Assert.Equal(0u, device.Cpu.R[4]);
        device.Cpu.Cycles++;
        scheduler.Step();
        Assert.Equal(1u, device.Cpu.R[3]);
        Assert.Equal(2u, device.Cpu.R[4]);
    }

    [Fact]
    public void TestWrappingScheduler()
    {
        var device = new Device(_bios, _testGamePak, new TestDebugger(), true);

        var scheduler = new Scheduler(device);

        for (var ii = 0; ii < 100; ii++)
        {
            scheduler.ScheduleEvent(EventType.Generic, &TestEventR4_2, 1);
            scheduler.ScheduleEvent(EventType.Generic, &TestEventR3_1, 1);
            device.Cpu.Cycles++;
            scheduler.Step();
            Assert.Equal(1u, device.Cpu.R[3]);
            Assert.Equal(2u, device.Cpu.R[4]);
        }
    }

    [Fact]
    public void TestCancelEvent()
    {
        var device = new Device(_bios, _testGamePak, new TestDebugger(), true);

        var scheduler = new Scheduler(device);

        scheduler.ScheduleEvent(EventType.HBlankStart, &TestEventR4_1, 1);
        scheduler.ScheduleEvent(EventType.Generic, &TestEventR4_2, 2);
        scheduler.CancelEvent(EventType.HBlankStart);
        Assert.Equal(0u, device.Cpu.R[4]);
        device.Cpu.Cycles++;
        scheduler.Step();
        Assert.Equal(0u, device.Cpu.R[4]);
        device.Cpu.Cycles++;
        scheduler.Step();
        Assert.Equal(2u, device.Cpu.R[4]);
    }

    [Fact]
    public void TestCancelLastEvent()
    {
        var device = new Device(_bios, _testGamePak, new TestDebugger(), true);

        var scheduler = new Scheduler(device);

        scheduler.ScheduleEvent(EventType.HBlankStart, &TestEventR4_1, 1);
        scheduler.ScheduleEvent(EventType.Generic, &TestEventR4_2, 2);
        scheduler.CancelEvent(EventType.Generic);
        Assert.Equal(0u, device.Cpu.R[4]);
        device.Cpu.Cycles++;
        scheduler.Step();
        Assert.Equal(1u, device.Cpu.R[4]);
        device.Cpu.Cycles++;
        scheduler.Step();
        Assert.Equal(1u, device.Cpu.R[4]);
    }

    [Fact]
    public void TestCancelNoEvent()
    {
        var device = new Device(_bios, _testGamePak, new TestDebugger(), true);

        var scheduler = new Scheduler(device);
        scheduler.CancelEvent(EventType.Generic);
        Assert.Equal(0u, device.Cpu.R[4]);
        device.Cpu.Cycles++;
        scheduler.Step();
        Assert.Equal(0u, device.Cpu.R[4]);
    }
}
