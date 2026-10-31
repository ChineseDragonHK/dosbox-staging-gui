namespace DosGameLauncher;

internal static class MapperFiles
{
    public static string Directory => AppDataStore.MappersDirectory;

    public static string PathFor(string gameId)
    {
        var safe = new string(gameId.Where(char.IsLetterOrDigit).ToArray());
        if (safe.Length == 0)
            safe = "game";
        return Path.Combine(Directory, safe + ".map");
    }

    public static string ConfigPath(string gameId) => PathFor(gameId).Replace('\\', '/');

    public static void Delete(string gameId)
    {
        var path = PathFor(gameId);
        if (File.Exists(path))
            File.Delete(path);
    }
}
