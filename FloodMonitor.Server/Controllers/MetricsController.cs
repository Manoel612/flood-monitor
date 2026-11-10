using FloodMonitor.Server.Data;
using FloodMonitor.Server.Models;
using FloodMonitor.Server.Models.Dtos;
using FloodMonitor.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FloodMonitor.Server.Controllers;

[ApiController]
[Route("api/metrics")]
public class MetricsController(AppDbContext db, TimeZoneInfo timeZone) : ControllerBase
{
    private const int NeighborhoodRankingSize = 10;
    private const int MaxHotspots = 20;
    private const int DecimalPlacesFor110MeterGrid = 3;
    private const int MinDaysFlooded = 2;

    [HttpGet("summary")]
    public async Task<MetricsSummaryDto> Summary(DateTime? from, DateTime? to, [FromQuery] LocationFilter location, CancellationToken ct)
    {
        var all = await LoadAsync(null, null, location, ct);
        var now = DateTime.UtcNow;
        var last7Days = all.Count(o => o.OccurredAt >= now.AddDays(-7));
        var last30Days = all.Count(o => o.OccurredAt >= now.AddDays(-30));

        var start = from?.ToUniversalTime();
        var end = to?.ToUniversalTime();
        var inPeriod = all
            .Where(o => (start is null || o.OccurredAt >= start) && (end is null || o.OccurredAt <= end))
            .ToList();

        var mostAffectedNeighborhood = inPeriod
            .Where(o => o.Neighborhood is not null)
            .GroupBy(o => o.Neighborhood)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        var byWaterLevel = Enum.GetValues<WaterLevel>()
            .Select(level => new WaterLevelTotalDto(level, inPeriod.Count(o => o.WaterLevel == level)))
            .ToList();

        return new MetricsSummaryDto(inPeriod.Count, last7Days, last30Days, mostAffectedNeighborhood, byWaterLevel);
    }

    [HttpGet("by-neighborhood")]
    public async Task<List<NeighborhoodTotalDto>> ByNeighborhood(DateTime? from, DateTime? to, [FromQuery] LocationFilter location, CancellationToken ct)
    {
        var occurrences = await LoadAsync(from, to, location, ct);

        return occurrences
            .Where(o => o.Neighborhood is not null)
            .GroupBy(o => o.Neighborhood!)
            .Select(g => new NeighborhoodTotalDto(g.Key, g.Count(), g.Max(o => o.OccurredAt)))
            .OrderByDescending(n => n.Total)
            .ThenBy(n => n.Neighborhood, TextHelpers.PortugueseAlphabeticalOrder)
            .Take(NeighborhoodRankingSize)
            .ToList();
    }

    [HttpGet("by-month")]
    public async Task<List<MonthTotalDto>> ByMonth([FromQuery] LocationFilter location, int months = 12, CancellationToken ct = default)
    {
        months = Math.Clamp(months, 1, 36);
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var firstMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-(months - 1));
        var start = TimeZoneInfo.ConvertTimeToUtc(firstMonth, timeZone);

        var occurrences = await LoadAsync(start, null, location, ct);

        var counts = occurrences
            .Select(o => (Date: TimeZoneInfo.ConvertTimeFromUtc(o.OccurredAt, timeZone), o.WaterLevel))
            .GroupBy(o => (o.Date.Year, o.Date.Month, o.WaterLevel))
            .ToDictionary(g => g.Key, g => g.Count());

        return Enumerable.Range(0, months)
            .Select(i => firstMonth.AddMonths(i))
            .Select(month =>
            {
                var byWaterLevel = Enum.GetValues<WaterLevel>()
                    .Select(level => new WaterLevelTotalDto(level, counts.GetValueOrDefault((month.Year, month.Month, level))))
                    .ToList();
                return new MonthTotalDto(month.Year, month.Month, byWaterLevel.Sum(l => l.Total), byWaterLevel);
            })
            .ToList();
    }

    [HttpGet("hotspots")]
    public async Task<List<HotspotDto>> Hotspots(DateTime? from, DateTime? to, [FromQuery] LocationFilter location, CancellationToken ct)
    {
        var occurrences = await LoadAsync(from, to, location, ct);

        return occurrences
            .GroupBy(o => (Latitude: Math.Round(o.Latitude, DecimalPlacesFor110MeterGrid), Longitude: Math.Round(o.Longitude, DecimalPlacesFor110MeterGrid)))
            .Select(g => new HotspotDto(
                g.Key.Latitude,
                g.Key.Longitude,
                g.Count(),
                g.Select(o => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(o.OccurredAt, timeZone))).Distinct().Count(),
                g.Where(o => o.Neighborhood is not null).GroupBy(o => o.Neighborhood).OrderByDescending(n => n.Count()).Select(n => n.Key).FirstOrDefault(),
                g.Max(o => o.OccurredAt)))
            .Where(h => h.DistinctDays >= MinDaysFlooded)
            .OrderByDescending(h => h.Occurrences)
            .Take(MaxHotspots)
            .ToList();
    }

    private record OccurrenceForMetrics(double Latitude, double Longitude, WaterLevel WaterLevel, string? Neighborhood, DateTime OccurredAt);

    private async Task<List<OccurrenceForMetrics>> LoadAsync(DateTime? from, DateTime? to, LocationFilter location, CancellationToken ct)
    {
        var rows = await db.Occurrences.AsNoTracking()
            .VisibleInPeriod(from, to)
            .InLocation(location)
            .Select(o => new OccurrenceForMetrics(o.Latitude, o.Longitude, o.WaterLevel, o.Neighborhood, o.OccurredAt))
            .ToListAsync(ct);

        return rows.Where(o => location.IsWithinRadius(o.Latitude, o.Longitude)).ToList();
    }
}
