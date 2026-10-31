namespace DosGameLauncher;

internal sealed class ShaderFilter(string id, string label)
{
    public string Id { get; } = id;
    public string Label { get; } = label;
    public override string ToString() => Label;
}

internal static class ShaderFilters
{
    public const string DefaultId = "crt-auto";

    public static readonly ShaderFilter[] All =
    [
        new("none", "不使用濾鏡"),
        new("crt-auto", "自動 CRT"),
        new("crt-auto-machine", "CRT，跟著顯示卡"),
        new("crt-auto-arcade", "街機 CRT"),
        new("crt-auto-arcade-sharp", "街機 CRT，線條較細"),
        new("crt/crt-hyllian", "Hyllian CRT"),
        new("crt/vga-1080p", "VGA 掃描線"),
        new("crt/vga-1080p-fake-double-scan", "VGA 雙掃描線"),
        new("interpolation/sharp", "銳利"),
        new("interpolation/nearest", "像素原點"),
        new("interpolation/bilinear", "模糊"),
        new("interpolation/catmull-rom", "平滑"),
        new("scaler/xbr-lv2-3d", "xBR 平滑"),
        new("scaler/xbr-lv2-noblend", "xBR 平滑，不混色"),
        new("scaler/xbr-lv3", "xBR 強平滑"),
        new("scaler/advmame2x", "AdvanceMAME 2x"),
        new("scaler/advmame3x", "AdvanceMAME 3x"),
        new("scaler/advinterp2x", "進階插值 2x"),
        new("scaler/advinterp3x", "進階插值 3x"),
    ];

    public static string Normalize(string? id) =>
        All.Any(filter => filter.Id == id) ? id! : DefaultId;
}
