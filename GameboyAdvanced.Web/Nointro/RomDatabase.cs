namespace GameboyAdvanced.Web.Nointro;

public class RomDatabase
{
    internal Dictionary<string, RomDatabaseEntry> RomEntries { get; private set; } = new();

    internal DirectoryTreeEntry? RomDirectoryTree { get; private set; }

    internal string? RomDirectoryError { get; private set; }

    internal static RomDatabase BuildDatabase(string baseDirectory)
    {
        var database = new RomDatabase();

        if (!Directory.Exists(baseDirectory))
        {
            database.RomDirectoryError = $"Directory {baseDirectory} does not exist";
            return database;
        }

        DirectoryTreeEntry BuildTree(DirectoryInfo baseDir)
        {
            var entry = new DirectoryTreeEntry(baseDir.Name);
            entry.RomTreeEntries.AddRange(
                baseDir.EnumerateFiles("*.gba").Select(romFileInfo => new RomDatabaseEntry(romFileInfo.Name, romFileInfo.FullName)));
            foreach (var rom in entry.RomTreeEntries)
            {
                database.RomEntries.Add(rom.Id.ToString(), rom);
            }

            entry.SubDirectories.AddRange(baseDir.EnumerateDirectories().Select(BuildTree));
            return entry;
        }

        database.RomDirectoryTree = BuildTree(new DirectoryInfo(baseDirectory));

        return database;
    }
}
