using System.Text.Json;
using System.Text.Json.Serialization;

namespace DosGameLauncher;

internal sealed class GameEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Executable { get; set; } = "";
    public bool Fullscreen { get; set; } = true;
    public bool FrameGeneration { get; set; }
    public int FrameMultiplier { get; set; } = 2;
    public string FrameMode { get; set; } = "fixed";
    public int FrameTarget { get; set; } = 60;
    public int FlowScale { get; set; } = 100;
    public string FrameQuality { get; set; } = "balanced";
    public string LsScaling { get; set; } = "off";
    public int Sharpness { get; set; } = 5;
    public string SyncMode { get; set; } = "default";
    public bool ShowFps { get; set; } = true;
    public string Speed { get; set; } = "auto";
    public int CustomCycles { get; set; } = CpuCycles.DefaultCustom;
    public string Shader { get; set; } = ShaderFilters.DefaultId;
    /// <summary>ISO/CUE 等光碟映像；啟動時掛載為 <see cref="CdDriveLetter"/>。</summary>
    public string CdImagePath { get; set; } = "";
    /// <summary>第二片光碟（同一光碟機換片，例如俠客英雄傳 3 的 SHK3_2.cue）。</summary>
    public string CdImagePath2 { get; set; } = "";
    public string CdDriveLetter { get; set; } = "d";
    public DateTime? LastPlayed { get; set; }

    [JsonIgnore]
    public bool FileMissing { get; set; }
}

internal sealed class GameLibrary
{
    public string DosboxPath { get; set; } = "";
    /// <summary>各遊戲速度為「自動」時的全局 cycles（<see cref="CpuCycles.NormalizeGlobal"/>）。</summary>
    public string GlobalCpuCycles { get; set; } = "";
    public string LastBrowseDirectory { get; set; } = "";
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }
    public List<GameEntry> Games { get; set; } = [];

    [JsonIgnore]
    public bool RecoveredFromCorruptFile { get; set; }
}

internal static class LibraryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string FilePath => AppDataStore.LibraryPath;

    public static GameLibrary Load()
    {
        var path = FilePath;
        if (!File.Exists(path))
            return CreateDefault();

        try
        {
            var library = JsonSerializer.Deserialize<GameLibrary>(File.ReadAllText(path), JsonOptions) ?? CreateDefault();
            library.Games ??= [];
            library.Games = library.Games
                .Where(game => !string.IsNullOrWhiteSpace(game.Executable))
                .ToList();
            library.GlobalCpuCycles = CpuCycles.NormalizeGlobal(library.GlobalCpuCycles);
            foreach (var game in library.Games)
            {
                if (string.IsNullOrWhiteSpace(game.Id))
                    game.Id = Guid.NewGuid().ToString("N");
                if (string.IsNullOrWhiteSpace(game.Name))
                    game.Name = SuggestName(game.Executable);
                if (game.Speed is not ("auto" or "slow" or "max" or "custom"))
                    game.Speed = "auto";
                game.CustomCycles = CpuCycles.ClampCustom(game.CustomCycles);
                game.Shader = ShaderFilters.Normalize(game.Shader);
                PictureOptions.Normalize(game);
                CdMount.NormalizePath(game);
            }

            if (string.IsNullOrWhiteSpace(library.DosboxPath) || !File.Exists(library.DosboxPath))
                library.DosboxPath = DetectDosbox();
            return library;
        }
        catch (JsonException)
        {
            File.Copy(path, path + ".bad", overwrite: true);
            var fresh = CreateDefault();
            fresh.RecoveredFromCorruptFile = true;
            return fresh;
        }
    }

    public static void Save(GameLibrary library)
    {
        var path = FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(library, JsonOptions));
        File.Move(temp, path, overwrite: true);
    }

    public static string DetectDosbox()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var app = AppContext.BaseDirectory;
        string[] candidates =
        [
            Path.Combine(app, "DOSBox Staging", "dosbox.exe"),
            Path.Combine(app, "dosbox.exe"),
            Path.Combine(local, "Programs", "DOSBox Staging", "dosbox.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DOSBox Staging", "dosbox.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "DOSBox Staging", "dosbox.exe"),
        ];
        return candidates.FirstOrDefault(File.Exists) ?? "";
    }

    public static string SuggestName(string executable)
    {
        var dir = Path.GetDirectoryName(executable);
        if (string.IsNullOrEmpty(dir))
            return Path.GetFileNameWithoutExtension(executable);

        var name = new DirectoryInfo(dir).Name;
        if (IsGenericFolder(name) && Directory.GetParent(dir) is { } parent)
            name = parent.Name;

        return string.IsNullOrWhiteSpace(name)
            ? Path.GetFileNameWithoutExtension(executable)
            : name;
    }

    private static bool IsGenericFolder(string name) =>
        name.Equals("bin", StringComparison.OrdinalIgnoreCase)
        || name.Equals("dosbox", StringComparison.OrdinalIgnoreCase)
        || name.Equals("game", StringComparison.OrdinalIgnoreCase)
        || name.Equals("app", StringComparison.OrdinalIgnoreCase);

    private static GameLibrary CreateDefault() => new()
    {
        DosboxPath = DetectDosbox(),
    };
}
