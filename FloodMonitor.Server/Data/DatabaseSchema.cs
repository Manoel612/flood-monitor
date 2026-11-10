using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace FloodMonitor.Server.Data;

public static partial class DatabaseSchema
{
    public static void CreateDatabaseOrMissingTables(AppDbContext db)
    {
        if (db.Database.EnsureCreated())
            return;

        var existingTables = db.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var statements = db.Database.GenerateCreateScript()
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var statement in statements)
        {
            var table = TargetTable(statement);
            if (table is not null && !existingTables.Contains(table))
                db.Database.ExecuteSqlRaw(statement);
        }
    }

    private static string? TargetTable(string statement)
    {
        var match = CreateTablePattern().Match(statement);
        if (!match.Success)
            match = CreateIndexPattern().Match(statement);
        return match.Success ? match.Groups["table"].Value : null;
    }

    [GeneratedRegex("""^CREATE TABLE "(?<table>[^"]+)" """, RegexOptions.IgnoreCase)]
    private static partial Regex CreateTablePattern();

    [GeneratedRegex("""^CREATE (UNIQUE )?INDEX "[^"]+" ON "(?<table>[^"]+)" """, RegexOptions.IgnoreCase)]
    private static partial Regex CreateIndexPattern();
}
