namespace GameboyAdvanced.Web.Nointro;

public class RomDatabaseEntry
{
    public Guid Id { get; }

    public string Name { get; }

    public string FullFilePath { get; }

    internal RomDatabaseEntry(string name, string fullFilePath)
    {
        Id = Guid.NewGuid();
        Name = name;
        FullFilePath = fullFilePath;
    }
}
