namespace DosGameLauncher;

internal static class LaunchTarget
{
    private static readonly HashSet<string> SetupTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "JS3", "SETUP", "INSTALL", "SETSOUND", "SETM", "SOUNDSET", "SETBLAST", "SNDSETUP",
    };

    private static readonly HashSet<string> IgnoredCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "ECHO", "REM", "CLS", "CD", "CHDIR", "PAUSE", "LH", "LOADHIGH", "SET",
    };

    public static string Resolve(string executable)
    {
        if (!executable.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) || !File.Exists(executable))
            return executable;

        var directory = Path.GetDirectoryName(executable);
        if (string.IsNullOrEmpty(directory))
            return executable;

        var programs = new List<string>();
        foreach (var raw in File.ReadAllLines(executable))
        {
            var line = raw.Trim();
            if (line.StartsWith("::", StringComparison.Ordinal))
                continue;
            if (line.StartsWith('@'))
                line = line[1..].Trim();
            if (line.Length == 0)
                continue;

            var token = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)[0].Trim('"');
            if (IgnoredCommands.Contains(token))
                continue;
            if (token.Contains(':') || token.Contains('\\') || token.Contains('/'))
                return executable;

            programs.Add(Path.GetFileNameWithoutExtension(token));
        }

        if (programs.Count == 0)
            return executable;

        var gamePath = FindProgram(directory, programs[^1]);
        if (gamePath == null)
            return executable;

        var onlySetupBeforeGame = programs
            .Take(programs.Count - 1)
            .All(program => SetupTools.Contains(program));
        return onlySetupBeforeGame ? gamePath : executable;
    }

    private static string? FindProgram(string directory, string name)
    {
        foreach (var ext in new[] { ".EXE", ".COM", ".BAT", ".exe", ".com", ".bat" })
        {
            var path = Path.Combine(directory, name + ext);
            if (File.Exists(path))
                return path;
        }

        return null;
    }
}
