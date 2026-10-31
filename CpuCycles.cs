namespace DosGameLauncher;

internal static class CpuCycles
{
    public const int MinCustom = 1000;
    public const int MaxCustom = 200_000;
    public const int DefaultCustom = 10_000;

    /// <summary>全遊戲預設（各遊戲速度＝自動時）。空字串＝不覆寫。</summary>
    public static string NormalizeGlobal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        var v = value.Trim();
        if (v.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return "auto";
        if (v.Equals("max", StringComparison.OrdinalIgnoreCase))
            return "max";
        if (v.Equals("3000", StringComparison.OrdinalIgnoreCase) || v.Equals("slow", StringComparison.OrdinalIgnoreCase))
            return "3000";
        if (int.TryParse(v, out var n))
            return ClampCustom(n).ToString();
        return "";
    }

    public static int ClampCustom(int value) =>
        Math.Clamp(value, MinCustom, MaxCustom);

    /// <summary>DOSBox Staging <c>--set cycles=…</c>；null 表示不傳（用 DOSBox 內建預設）。</summary>
    public static string? ForLaunch(GameEntry game, GameLibrary library)
    {
        switch (game.Speed)
        {
            case "slow":
                return "3000";
            case "max":
                return "max";
            case "custom":
                return ClampCustom(game.CustomCycles).ToString();
        }

        var global = NormalizeGlobal(library.GlobalCpuCycles);
        if (global.Length == 0 || global.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return null;
        return global;
    }
}
