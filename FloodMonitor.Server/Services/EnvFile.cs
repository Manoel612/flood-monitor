namespace FloodMonitor.Server.Services;

public static class EnvFile
{
    private const int FoldersToSearch = 3;

    public static string? LoadForCurrentEnvironment()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";

        var path = Find($".env.{environment.ToLowerInvariant()}");
        if (path is null) return null;

        foreach (var line in File.ReadAllLines(path))
        {
            var (key, value) = Parse(line);
            if (key is null || value.Length == 0) continue;
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
        return path;
    }

    private static string? Find(string fileName)
    {
        var folder = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var i = 0; i < FoldersToSearch && folder is not null; i++, folder = folder.Parent)
        {
            var candidate = Path.Combine(folder.FullName, fileName);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private static (string? Key, string Value) Parse(string line)
    {
        var text = line.Trim();
        if (text.Length == 0 || text.StartsWith('#')) return (null, "");

        var separator = text.IndexOf('=');
        if (separator <= 0) return (null, "");

        var key = text[..separator].Trim();
        var value = text[(separator + 1)..].Trim();
        if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0])
            value = value[1..^1];
        return (key, value);
    }
}
