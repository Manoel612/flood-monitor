using System.Globalization;
using System.Text.Json.Serialization;

namespace FloodMonitor.Server.Services.Rainfall;

public record DailyPrecipitation(DateOnly Day, double Millimeters);

public class OpenMeteoClient(IHttpClientFactory httpClientFactory)
{
    public const string HttpClientName = "open-meteo";

    public async Task<List<DailyPrecipitation>> GetDailyPrecipitationAsync(
        double latitude, double longitude, DateOnly firstDay, DateOnly lastDay, string timeZoneId, CancellationToken ct)
    {
        var url = string.Create(CultureInfo.InvariantCulture,
            $"v1/archive?latitude={latitude:F4}&longitude={longitude:F4}" +
            $"&start_date={firstDay:yyyy-MM-dd}&end_date={lastDay:yyyy-MM-dd}" +
            $"&daily=precipitation_sum&timezone={Uri.EscapeDataString(timeZoneId)}");

        var response = await httpClientFactory.CreateClient(HttpClientName).GetFromJsonAsync<ArchiveResponse>(url, ct);
        var daily = response?.Daily;
        if (daily is null)
            return [];

        return daily.Time
            .Zip(daily.PrecipitationSum)
            .Where(pair => pair.Second is not null)
            .Select(pair => new DailyPrecipitation(
                DateOnly.ParseExact(pair.First, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                Math.Round(pair.Second!.Value, 1)))
            .ToList();
    }

    private record ArchiveResponse(ArchiveDaily? Daily);

    private record ArchiveDaily(
        List<string> Time,
        [property: JsonPropertyName("precipitation_sum")] List<double?> PrecipitationSum);
}
