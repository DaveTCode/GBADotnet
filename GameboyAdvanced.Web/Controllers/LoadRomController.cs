using GameboyAdvanced.Web.Emulation;
using GameboyAdvanced.Web.Nointro;
using Microsoft.AspNetCore.Mvc;

namespace GameboyAdvanced.Web.Controllers;

[Route("/api/v1/rom")]
public class LoadRomController : Controller
{
    private readonly ILogger<LoadRomController> _logger;
    private readonly BackgroundEmulatorThread _backgroundEmulatorThread;
    private readonly RomDatabase _romDatabase;

    public LoadRomController(ILogger<LoadRomController> logger, BackgroundEmulatorThread backgroundEmulatorThread, RomDatabase romDatabase)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _backgroundEmulatorThread = backgroundEmulatorThread ?? throw new ArgumentNullException(nameof(backgroundEmulatorThread));
        _romDatabase = romDatabase ?? throw new ArgumentNullException(nameof(romDatabase));
    }

    [Route("load"), HttpPost]
    public async Task<IActionResult> LoadRom(string guid)
    {
        _logger.LogInformation("Load rom called for guid {guid}", guid);

        if (!_romDatabase.RomEntries.TryGetValue(guid, out var romDatabaseEntry))
        {
            return NotFound($"Rom not found with GUID {guid}");
        }

        var romBytes = await System.IO.File.ReadAllBytesAsync(romDatabaseEntry.FullFilePath);

        await _backgroundEmulatorThread.RunRomAsync(romBytes);
        
        _logger.LogInformation("Rom loaded");
        return Ok();
    }
}
