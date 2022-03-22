namespace GameboyAdvanced.Web.Nointro;

public class DirectoryTreeEntry
{
    public Guid Id { get; }

    public string Name { get; }

    public List<RomDatabaseEntry> RomTreeEntries { get; } = new();

    public List<DirectoryTreeEntry> SubDirectories { get; } = new();

    internal DirectoryTreeEntry(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }
}
