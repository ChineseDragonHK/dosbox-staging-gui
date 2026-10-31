using System.Diagnostics;

namespace DosGameLauncher;

internal static class CdMount
{
    public static void Validate(GameEntry game)
    {
        if (string.IsNullOrWhiteSpace(game.CdImagePath))
            return;
        if (!File.Exists(game.CdImagePath))
            throw new InvalidOperationException("找不到光碟映像：\n" + game.CdImagePath);
        if (!string.IsNullOrWhiteSpace(game.CdImagePath2) && !File.Exists(game.CdImagePath2))
            throw new InvalidOperationException("找不到第二片光碟映像：\n" + game.CdImagePath2);
    }

    public static void AddToLaunch(ProcessStartInfo start, GameEntry game)
    {
        if (string.IsNullOrWhiteSpace(game.CdImagePath))
            return;

        var drive = NormalizeDriveLetter(game.CdDriveLetter);
        var path1 = ToHostPath(game.CdImagePath);
        var command = string.IsNullOrWhiteSpace(game.CdImagePath2)
            ? $"mount {drive} \"{path1}\" -t cdrom"
            : $"mount {drive} \"{path1}\" \"{ToHostPath(game.CdImagePath2)}\" -t cdrom";
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add(command);
    }

    private static string ToHostPath(string path) =>
        Path.GetFullPath(path).Replace('\\', '/');

    public static string NormalizeDriveLetter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "d";
        var c = char.ToLowerInvariant(value.Trim()[0]);
        return c is >= 'c' and <= 'y' ? c.ToString() : "d";
    }

    public static void NormalizePath(GameEntry game)
    {
        game.CdImagePath = game.CdImagePath?.Trim() ?? "";
        game.CdImagePath2 = game.CdImagePath2?.Trim() ?? "";
        game.CdDriveLetter = NormalizeDriveLetter(game.CdDriveLetter);
    }
}
