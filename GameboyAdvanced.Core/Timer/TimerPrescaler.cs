namespace GameboyAdvanced.Core.Timer;

internal enum TimerPrescaler
{
    F_1 = 0b00,
    F_64 = 0b01,
    F_256 = 0b10,
    F_1024 = 0b11,
}

internal static class TimerPrescalerExtensions
{
    internal static int Cycles(this TimerPrescaler prescaler) => prescaler switch
    {
        TimerPrescaler.F_1 => 1,
        TimerPrescaler.F_64 => 64,
        TimerPrescaler.F_256 => 256,
        TimerPrescaler.F_1024 => 1024,
        _ => throw new ArgumentOutOfRangeException(nameof(prescaler)),
    };
}
