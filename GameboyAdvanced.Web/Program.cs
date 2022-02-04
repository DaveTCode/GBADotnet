using GameboyAdvanced.Web.Emulation;
using GameboyAdvanced.Web.Signalr;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

var biosPath = builder.Configuration.GetValue<string>("Bios");
var bios = File.ReadAllBytes(biosPath);

builder.Services.AddControllers();
builder.Services.AddLogging();
builder.Services.AddSignalR();
builder.Services.AddSingleton<BackgroundEmulatorThread>(
    provider => new BackgroundEmulatorThread(
        provider.GetService<ILogger<BackgroundEmulatorThread>>() ?? throw new ArgumentNullException(),
        provider.GetService<IHubContext<EmulatorHub, IEmulatorClient>>() ?? throw new ArgumentNullException(),
        bios)
);
builder.Services.AddHostedService<BackgroundEmulatorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseSerilogRequestLogging();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<EmulatorHub>("/emulatorhub");
});

app.Run();
