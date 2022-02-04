using GameboyAdvanced.Web.Emulation;
using Microsoft.AspNetCore.Mvc;

namespace GameboyAdvanced.Web.Controllers;

[Route("/api/v1/rom")]
public class LoadRomController : Controller
{
    private readonly ILogger<LoadRomController> _logger;
    private readonly BackgroundEmulatorThread _backgroundEmulatorThread;

    public LoadRomController(ILogger<LoadRomController> logger, BackgroundEmulatorThread backgroundEmulatorThread)
    {
        _logger = logger;
        _backgroundEmulatorThread = backgroundEmulatorThread;
    }

    [Route("load"), HttpPost]
    public async Task<IActionResult> LoadRom(List<IFormFile> roms)
    {
        if (roms == null) return BadRequest("No rom specified");
        if (roms.Count != 1) return BadRequest("Can only load a single rom");
        
        var rom = roms[0];
        _logger.LogInformation($"Load rom called with {rom.Name}");

        using var ms = new MemoryStream();
        await rom.CopyToAsync(ms);
        await _backgroundEmulatorThread.RunRomAsync(ms.ToArray());
        
        return Ok();
    }
}
