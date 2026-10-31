namespace DosGameLauncher;

/// <summary>設定與清單只存放在主程式（DosGameLauncher.exe）所在資料夾。</summary>
internal static class AppDataStore
{
    public static string DataDirectory => AppContext.BaseDirectory;

    public static string LibraryPath => Path.Combine(DataDirectory, "library.json");

    public static string MappersDirectory => Path.Combine(DataDirectory, "mappers");
}
