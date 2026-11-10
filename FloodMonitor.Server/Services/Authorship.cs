using System.Security.Cryptography;
using System.Text;

namespace FloodMonitor.Server.Services;

public static class Authorship
{
    public const string Header = "X-Author";

    public static string? Hash(string? authorId)
    {
        if (string.IsNullOrWhiteSpace(authorId) || authorId.Length > 64)
            return null;

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(authorId.Trim())));
    }
}
