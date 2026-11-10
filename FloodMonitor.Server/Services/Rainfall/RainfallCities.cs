using FloodMonitor.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace FloodMonitor.Server.Services.Rainfall;

public record RainfallCity(string City, string State, double Latitude, double Longitude, int Occurrences);

public static class RainfallCities
{
    public static async Task<List<RainfallCity>> WithOccurrencesAsync(AppDbContext db, CancellationToken ct)
    {
        var groups = await db.Occurrences.AsNoTracking()
            .Where(o => !o.IsHidden && o.City != null && o.State != null)
            .GroupBy(o => new { o.City, o.State })
            .Select(g => new
            {
                g.Key.City,
                g.Key.State,
                Latitude = g.Average(o => o.Latitude),
                Longitude = g.Average(o => o.Longitude),
                Occurrences = g.Count()
            })
            .ToListAsync(ct);

        return groups
            .Select(g => new RainfallCity(g.City!, g.State!, g.Latitude, g.Longitude, g.Occurrences))
            .OrderByDescending(c => c.Occurrences)
            .ThenBy(c => c.City, TextHelpers.PortugueseAlphabeticalOrder)
            .ToList();
    }
}
