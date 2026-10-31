using System.Diagnostics;

namespace DosGameLauncher;

internal static class GameRunner
{
    public static Process Launch(GameLibrary library, GameEntry game)
    {
        if (!File.Exists(library.DosboxPath))
            throw new InvalidOperationException("找不到 DOSBox Staging。請到右上角「設定」指定 dosbox.exe。");

        if (!File.Exists(game.Executable))
            throw new InvalidOperationException("找不到遊戲執行檔：\n" + game.Executable);

        CdMount.Validate(game);

        var directory = Path.GetDirectoryName(game.Executable);
        if (string.IsNullOrEmpty(directory))
            throw new InvalidOperationException("遊戲路徑不正確。");

        var start = new ProcessStartInfo(library.DosboxPath)
        {
            UseShellExecute = false,
            WorkingDirectory = directory,
        };
        start.ArgumentList.Add("--nolocalconf");
        start.ArgumentList.Add("--working-dir");
        start.ArgumentList.Add(directory);
        var windowed = !game.Fullscreen;
        start.ArgumentList.Add("--set");
        start.ArgumentList.Add(windowed ? "fullscreen=off" : "fullscreen=on");
        if (windowed)
        {
            start.ArgumentList.Add("--set");
            start.ArgumentList.Add("aspect=auto");
            start.ArgumentList.Add("--set");
            start.ArgumentList.Add("integer_scaling=off");
            start.ArgumentList.Add("--set");
            start.ArgumentList.Add("viewport=fit");
        }
        start.ArgumentList.Add("--set");
        start.ArgumentList.Add("output=opengl");
        foreach (var pair in PictureOptions.DosBoxLaunchSets(game))
        {
            start.ArgumentList.Add("--set");
            start.ArgumentList.Add(pair);
        }
        start.ArgumentList.Add("--set");
        start.ArgumentList.Add("shader=" + ShaderFilters.Normalize(game.Shader));
        if (CpuCycles.ForLaunch(game, library) is { } cycles)
        {
            start.ArgumentList.Add("--set");
            start.ArgumentList.Add("cycles=" + cycles);
        }
        Directory.CreateDirectory(MapperFiles.Directory);
        start.ArgumentList.Add("--set");
        start.ArgumentList.Add("mapperfile=" + MapperFiles.ConfigPath(game.Id));
        CdMount.AddToLaunch(start, game);
        var launchTarget = LaunchTarget.Resolve(game.Executable);
        // Staging：--working-dir 只影響設定檔路徑，不會掛載 C:。尾端 PATH 為 .exe/.bat 時會掛載其目錄為 C: 並啟動（見官方 command-line 說明）。
        start.ArgumentList.Add(launchTarget);

        return Process.Start(start) ?? throw new InvalidOperationException("無法啟動 DOSBox Staging。");
    }

    public static Process OpenMapper(GameLibrary library, GameEntry game)
    {
        if (!File.Exists(library.DosboxPath))
            throw new InvalidOperationException("找不到 DOSBox Staging。請到右上角「設定」指定 dosbox.exe。");

        Directory.CreateDirectory(MapperFiles.Directory);
        var start = new ProcessStartInfo(library.DosboxPath)
        {
            UseShellExecute = false,
        };
        start.ArgumentList.Add("--startmapper");
        start.ArgumentList.Add("--set");
        start.ArgumentList.Add("fullscreen=off");
        start.ArgumentList.Add("--set");
        start.ArgumentList.Add("mapperfile=" + MapperFiles.ConfigPath(game.Id));
        return Process.Start(start) ?? throw new InvalidOperationException("無法打開按鍵設定。");
    }

}
