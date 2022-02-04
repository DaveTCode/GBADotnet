namespace GameboyAdvanced.Web.Emulation;

public class BackgroundEmulatorService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BackgroundEmulatorService> _logger;

    public BackgroundEmulatorService(IServiceProvider services, ILogger<BackgroundEmulatorService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await DoWork(stoppingToken);
    }

    private async Task DoWork(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting background emulation service");

        using var scope = _services.CreateScope();

        var backgroundEmulatorThread =
            scope.ServiceProvider
                .GetRequiredService<BackgroundEmulatorThread>();

        await backgroundEmulatorThread.DoWorkAsync(stoppingToken);
    }
}
