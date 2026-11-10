using FloodMonitor.Server.Data;
using FloodMonitor.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace FloodMonitor.Server.Services.Rainfall;

public class RainfallSyncService(
    IServiceScopeFactory scopeFactory,
    OpenMeteoClient openMeteo,
    CemadenClient cemaden,
    IbgeMunicipalities ibge,
    TimeZoneInfo timeZone,
    IConfiguration configuration,
    ILogger<RainfallSyncService> logger) : BackgroundService
{
    public const int HistoryInDays = 366;
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan CemadenPublishingDelay = TimeSpan.FromHours(6);

    private readonly string timeZoneId = configuration["TimeZone"] ?? "America/Sao_Paulo";
    private readonly SemaphoreSlim wakeUp = new(0, 1);

    public bool IsEnabled { get; } = configuration.GetValue("Rainfall:Enabled", true);

    public bool IsCemadenEnabled => IsEnabled && cemaden.IsConfigured;

    public void RequestSync()
    {
        if (wakeUp.CurrentCount > 0)
            return;
        try
        {
            wakeUp.Release();
        }
        catch (SemaphoreFullException)
        {
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!IsEnabled)
            return;

        await Task.Yield();
        if (!cemaden.IsConfigured)
            logger.LogInformation("Cemaden:Email and Cemaden:Password are not set, so only Open-Meteo estimates will be collected.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllCitiesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Rainfall sync failed");
            }

            try
            {
                await wakeUp.WaitAsync(SyncInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task SyncAllCitiesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cities = await RainfallCities.WithOccurrencesAsync(db, ct);

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var yesterday = DateOnly.FromDateTime(localNow).AddDays(-1);

        foreach (var city in cities)
        {
            await RunSafelyAsync(RainfallSource.OpenMeteo, city, () => SyncOpenMeteoAsync(db, city, yesterday, ct));

            if (cemaden.IsConfigured)
            {
                var lastCompleteDay = DateOnly.FromDateTime(localNow - CemadenPublishingDelay).AddDays(-1);
                await RunSafelyAsync(RainfallSource.Cemaden, city, () => SyncCemadenAsync(db, city, lastCompleteDay, ct));
            }
        }
    }

    private async Task RunSafelyAsync(RainfallSource source, RainfallCity city, Func<Task> sync)
    {
        try
        {
            await sync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Could not sync {Source} rainfall for {City}, {State}", source, city.City, city.State);
        }
    }

    private async Task SyncOpenMeteoAsync(AppDbContext db, RainfallCity city, DateOnly lastDay, CancellationToken ct)
    {
        var firstDay = lastDay.AddDays(-(HistoryInDays - 1));
        var days = await openMeteo.GetDailyPrecipitationAsync(city.Latitude, city.Longitude, firstDay, lastDay, timeZoneId, ct);

        var existingByDay = await db.RainfallReadings
            .Where(r => r.Source == RainfallSource.OpenMeteo && r.City == city.City && r.State == city.State && r.Day >= firstDay)
            .ToDictionaryAsync(r => r.Day, ct);

        var now = DateTime.UtcNow;
        foreach (var day in days)
        {
            if (!existingByDay.TryGetValue(day.Day, out var reading))
            {
                reading = new RainfallReading { Source = RainfallSource.OpenMeteo, City = city.City, State = city.State, Day = day.Day };
                db.RainfallReadings.Add(reading);
            }

            reading.Latitude = Math.Round(city.Latitude, 4);
            reading.Longitude = Math.Round(city.Longitude, 4);
            reading.PrecipitationMm = day.Millimeters;
            reading.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        logger.LogInformation("Open-Meteo: {Days} days of rainfall saved for {City}, {State}", days.Count, city.City, city.State);
    }

    private async Task SyncCemadenAsync(AppDbContext db, RainfallCity city, DateOnly lastDay, CancellationToken ct)
    {
        var ibgeCode = await ibge.FindCodeAsync(city.City, city.State, ct);
        if (ibgeCode is null)
        {
            logger.LogWarning("No IBGE code found for {City}, {State}; skipping CEMADEN", city.City, city.State);
            return;
        }

        var firstDay = lastDay.AddDays(-(HistoryInDays - 1));
        var checkedDays = (await db.RainfallCheckedDays
                .Where(d => d.Source == RainfallSource.Cemaden && d.City == city.City && d.State == city.State && d.Day >= firstDay)
                .Select(d => d.Day)
                .ToListAsync(ct))
            .ToHashSet();

        var pendingDays = Enumerable.Range(0, HistoryInDays)
            .Select(offset => lastDay.AddDays(-offset))
            .Where(day => !checkedDays.Contains(day))
            .ToList();
        if (pendingDays.Count == 0)
            return;

        var stations = (await cemaden.GetRainGaugesAsync(ibgeCode.Value, ct))
            .DistinctBy(s => s.Code)
            .ToDictionary(s => s.Code);

        foreach (var day in pendingDays)
        {
            var endOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone);
            var totals = await cemaden.GetLast24HourTotalsAsync(ibgeCode.Value, endOfDayUtc, ct);
            var now = DateTime.UtcNow;

            foreach (var total in totals.DistinctBy(t => t.StationCode))
            {
                if (!stations.TryGetValue(total.StationCode, out var station))
                    continue;

                db.RainfallReadings.Add(new RainfallReading
                {
                    Source = RainfallSource.Cemaden,
                    City = city.City,
                    State = city.State,
                    StationCode = station.Code,
                    StationName = station.Name,
                    Latitude = station.Latitude,
                    Longitude = station.Longitude,
                    Day = day,
                    PrecipitationMm = total.Millimeters,
                    UpdatedAt = now
                });
            }

            db.RainfallCheckedDays.Add(new RainfallCheckedDay { Source = RainfallSource.Cemaden, City = city.City, State = city.State, Day = day });
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }

        logger.LogInformation("CEMADEN: {Days} days checked for {City}, {State}", pendingDays.Count, city.City, city.State);
    }
}
