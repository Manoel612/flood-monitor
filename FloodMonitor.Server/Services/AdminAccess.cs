using System.Security.Cryptography;
using System.Text;

namespace FloodMonitor.Server.Services;

public class AdminAccess
{
    private const int MinPasswordLength = 12;
    private const int MinTokenLength = 32;

    private readonly byte[]? usernameHash;
    private readonly byte[]? passwordHash;
    private readonly byte[]? tokenHash;

    public string? Token { get; }

    public bool IsConfigured => Token is not null;

    public AdminAccess(IConfiguration configuration, ILogger<AdminAccess> logger)
    {
        var username = configuration["Admin:Username"]?.Trim();
        var password = configuration["Admin:Password"];
        var token = configuration["Admin:Token"]?.Trim();

        if (string.IsNullOrEmpty(username) ||
            string.IsNullOrEmpty(password) || password.Length < MinPasswordLength ||
            string.IsNullOrEmpty(token) || token.Length < MinTokenLength)
        {
            logger.LogWarning(
                "Admin:Username, Admin:Password (at least {MinPasswordLength} characters) and Admin:Token " +
                "(at least {MinTokenLength} characters) must all be set. The admin area is disabled.",
                MinPasswordLength, MinTokenLength);
            return;
        }

        usernameHash = HashOf(username);
        passwordHash = HashOf(password);
        tokenHash = HashOf(token);
        Token = token;
    }

    public bool AreValidCredentials(string? username, string? password)
    {
        if (!IsConfigured)
            return false;

        var usernameMatches = CryptographicOperations.FixedTimeEquals(HashOf(username?.Trim() ?? ""), usernameHash);
        var passwordMatches = CryptographicOperations.FixedTimeEquals(HashOf(password ?? ""), passwordHash);
        return usernameMatches & passwordMatches;
    }

    public bool IsValidToken(string? token) =>
        IsConfigured && CryptographicOperations.FixedTimeEquals(HashOf(token?.Trim() ?? ""), tokenHash);

    private static byte[] HashOf(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
