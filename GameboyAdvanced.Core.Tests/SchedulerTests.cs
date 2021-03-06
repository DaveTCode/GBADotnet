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

    internal static void TestRecurseEventR4_Inc(Device device)
    {
        device.Cpu.R[4]++;

        device.Scheduler.ScheduleEvent(EventType.Generic, &TestRecurseEventR4_Inc, 1);
    }

    [Fact]
    public void TestAddEventToScheduler()
    {
        var device = new Device(_bios, _testGamePak, new TestDebugger(), true);

        device.Scheduler.ScheduleEvent(EventType.Generic, &TestEventR4_1, 2);
        Assert.Equal(0u, device.Cpu.R[4]);
        device.Cpu.Cycles++;
        device.Scheduler.Step();
        Assert.Equal(0u, device.Cpu.R[4]);
        device.Cpu.Cycles++;
        device.Scheduler.Step();
        Assert.Equal(1u, device.Cpu.R[4]);
    }

    [Fact]
    public void TestAddTwoEventsToScheduler()
    {
        var device = new Device(_bios, _testGamePak, new TestDebugger(), true);

        device.Scheduler.ScheduleEvent(EventType.Timer0Latch, &TestEventR4_1, 2);
        device.Scheduler.ScheduleEvent(EventType.Timer1Latch, &TestEventR4_2, 1);
        Assert.Equal(0u, device.Cpu.R[4]);
        device.Cpu.Cycles++;
        device.Scheduler.Step();
        Assert.Equal(2u, device.Cpu.R[4]);
        device.Cpu.Cycles++;
        device.Scheduler.Step();
        Assert.Equal(1u, device.Cpu.R[4]);
    }

    [Fact]
    public void TestAddEventsOnSameCycle()
    {
        var device = new Device(_bios, _testGamePak, new TestDebugger(), true);

        device.Scheduler.ScheduleEvent(EventType.Timer0Latch, &TestEventR4_2, 1);
        device.Scheduler.ScheduleEvent(EventType.Timer1Latch, &TestEventR3_1, 1);
        Assert.Equal(0u, device.Cpu.R[3]);
        Assert.Equal(0u, device.Cpu.R[4]);
        device.Cpu.Cycles++;
        device.Scheduler.Step();
        Assert.Equal(1u, device.Cpu.R[3]);
        Assert.Equal(2u, device.Cpu.R[4]);
    }

    [Fact]
    public void TestWrappingScheduler()
    {
        var device = new Device(_bios, _testGamePak, new TestDebugger(), true);

        for (var ii = 0; ii < 100; ii++)
        {
            device.Scheduler.ScheduleEvent(EventType.Timer0Latch, &TestEventR4_2, 1);
            device.Scheduler.ScheduleEvent(EventType.Timer1Latch, &TestEventR3_1, 1);
            device.Cpu.Cycles++;
            device.Scheduler.Step();
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



    [Fact]
    public void TestRecursivelyScheduleEvent()
    {
        var device = new Device(_bios, _testGamePak, new TestDebugger(), true);

        device.Scheduler.ScheduleEvent(EventType.Generic, &TestRecurseEventR4_Inc, 1);

        for (var ii = 0u; ii < 1000u; ii++)
        {
            Assert.Equal(ii, device.Cpu.R[4]);
            device.Cpu.Cycles++;
            device.Scheduler.Step();
        }
    }
}
