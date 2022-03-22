using GameboyAdvanced.Web.Nointro;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GameboyAdvanced.Web.Pages;

public class IndexModel : PageModel
{
    private readonly string _romDirectory;

    public string? RomDirectoryError { get; }

    public DirectoryTreeEntry? RomDirectoryTree { get; }

    public IndexModel(IConfiguration config, RomDatabase romDatabase)
    {
        _romDirectory = config.GetValue<string>("RomDirectory");
        RomDirectoryTree = romDatabase.RomDirectoryTree;
        RomDirectoryError = romDatabase.RomDirectoryError;
    }

    public PageResult OnGet()
    {
        return Page();
    }
}
