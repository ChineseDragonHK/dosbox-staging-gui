namespace DosGameLauncher;

internal sealed class Choice(string id, string label)
{
    public string Id { get; } = id;
    public string Label { get; } = label;
    public override string ToString() => Label;
}

internal static class PictureOptions
{
    public static readonly Choice[] SyncModes =
    [
        new("default", "預設"),
        new("vsync", "垂直同步（較少撕裂）"),
        new("off", "關閉（選單較跟手，建議）"),
    ];

    public static void Normalize(GameEntry game)
    {
        game.FrameGeneration = false;
        game.LsScaling = "off";
        game.SyncMode = game.SyncMode is "vsync" or "off" ? game.SyncMode : "default";
    }

    /// <summary>DOSBox Staging <c>--set</c> pairs（啟動用 <c>--nolocalconf</c> 時必須明確傳入）。</summary>
    public static IEnumerable<string> DosBoxLaunchSets(GameEntry game)
    {
        Normalize(game);
        switch (game.SyncMode)
        {
            case "vsync":
                yield return "vsync=on";
                yield return "presentation_mode=host-rate";
                break;
            case "off":
                yield return "vsync=off";
                yield return "presentation_mode=dos-rate";
                break;
            default:
                yield return "vsync=off";
                yield return "presentation_mode=auto";
                break;
        }

        yield return "mouse_raw_input=on";
    }
}
