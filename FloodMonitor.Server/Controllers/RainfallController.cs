using FloodMonitor.Server.Data;
using FloodMonitor.Server.Models;
using FloodMonitor.Server.Models.Dtos;
using FloodMonitor.Server.Services.Rainfall;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FloodMonitor.Server.Controllers;

[ApiController]
[Route("api/rainfall")]
public class RainfallController(AppDbContext db, RainfallSyncService sync, TimeZoneInfo timeZone) : ControllerBase
{
    private const int MonthsShown = 12;

    [HttpGet("cities")]
    public async Task<List<RainfallCityDto>> Cities(CancellationToken ct) =>
        (await RainfallCities.WithOccurrencesAsync(db, ct))
            .Select(c => new RainfallCityDto(c.City, c.State, c.Latitude, c.Longitude, c.Occurrences))
            .ToList();

    [HttpGet("monthly")]
    public async Task<ActionResult<CityRainfallDto>> Monthly(string city, string state, CancellationToken ct)
    {
        var place = (await RainfallCities.WithOccurrencesAsync(db, ct))
            .FirstOrDefault(c => c.City == city && c.State == state);
        if (place is null)
            return NotFound();

        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var firstMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-(MonthsShown - 1));
        var months = Enumerable.Range(0, MonthsShown).Select(i => firstMonth.AddMonths(i)).ToList();

        var readings = await db.RainfallReadings.AsNoTracking()
            .Where(r => r.City == city && r.State == state && r.Day >= firstMonth)
            .ToListAsync(ct);

        var estimatedReadings = readings.Where(r => r.Source == RainfallSource.OpenMeteo).ToList();
        var estimated = estimatedReadings.Count == 0
            ? null
            : new RainfallSeriesDto(RainfallSource.OpenMeteo, null, MonthlyTotals(estimatedReadings, months));

        var stations = readings
            .Where(r => r.Source == RainfallSource.Cemaden)
            .GroupBy(r => r.StationCode)
            .Select(g =>
            {
                var first = g.First();
                var distanceKm = OccurrenceQueries.DistanceInMeters(place.Latitude, place.Longitude, first.Latitude, first.Longitude) / 1000;
                return new RainfallStationDto(first.StationCode, first.StationName ?? first.StationCode,
                    first.Latitude, first.Longitude, Math.Round(distanceKm, 1), g.Count());
            })
            .OrderBy(s => s.DistanceKm)
            .ToList();

        var nearestStation = stations.FirstOrDefault();
        var measured = nearestStation is null
            ? null
            : new RainfallSeriesDto(RainfallSource.Cemaden, nearestStation, MonthlyTotals(
                readings.Where(r => r.Source == RainfallSource.Cemaden && r.StationCode == nearestStation.Code).ToList(), months));

        var occurrenceDates = await db.Occurrences.AsNoTracking()
            .Where(o => !o.IsHidden && o.City == city && o.State == state)
            .Select(o => o.OccurredAt)
            .ToListAsync(ct);
        var occurrencesByMonth = occurrenceDates
            .Select(utc => TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone))
            .GroupBy(local => (local.Year, local.Month))
            .ToDictionary(g => g.Key, g => g.Count());
        var occurrences = months
            .Select(m => new MonthTotalOccurrencesDto(m.Year, m.Month, occurrencesByMonth.GetValueOrDefault((m.Year, m.Month))))
            .ToList();

        var syncPending = sync.IsEnabled && estimated is null;
        if (syncPending)
            sync.RequestSync();

        return new CityRainfallDto(place.City, place.State, place.Latitude, place.Longitude,
            measured, estimated, stations, occurrences, sync.IsCemadenEnabled, syncPending);
    }

    private static List<MonthlyRainfallDto> MonthlyTotals(List<RainfallReading> readings, List<DateOnly> months)
    {
        var byMonth = readings
            .GroupBy(r => (r.Day.Year, r.Day.Month))
            .ToDictionary(g => g.Key, g => (Millimeters: g.Sum(r => r.PrecipitationMm), Days: g.Count()));

        return months
            .Select(m => byMonth.TryGetValue((m.Year, m.Month), out var total)
                ? new MonthlyRainfallDto(m.Year, m.Month, Math.Round(total.Millimeters, 1), total.Days)
                : new MonthlyRainfallDto(m.Year, m.Month, null, 0))
            .ToList();
    }
}
