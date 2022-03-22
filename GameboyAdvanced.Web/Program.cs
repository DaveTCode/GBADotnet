using GameboyAdvanced.Web.Emulation;
using GameboyAdvanced.Web.Nointro;
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

// Load the bios file
var biosPath = builder.Configuration.GetValue<string>("Bios");
var bios = File.ReadAllBytes(biosPath);

// Build the database of known ROMs
var database = RomDatabase.BuildDatabase(builder.Configuration.GetValue<string>("RomDirectory"));

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddLogging();
builder.Services.AddSignalR().AddJsonProtocol(options => {
    options.PayloadSerializerOptions.IncludeFields = true; // We use fields not properties across the emulator
});
builder.Services.AddSingleton<RomDatabase>(database);
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
    _ = app.UseExceptionHandler("/Error");
}

_ = app.UseSerilogRequestLogging();
_ = app.UseStaticFiles();

_ = app.UseRouting();

_ = app.UseEndpoints(endpoints =>
{
    _ = endpoints.MapRazorPages();
    _ = endpoints.MapControllers();
    _ = endpoints.MapHub<EmulatorHub>("/emulatorhub");
});

app.Run();
