using System.Globalization;

namespace FloodMonitor.Server.Services;

public static class TextHelpers
{
    public static readonly StringComparer PortugueseAlphabeticalOrder =
        StringComparer.Create(new CultureInfo("pt-BR"), ignoreCase: true);

    public static string? TrimOrNull(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
