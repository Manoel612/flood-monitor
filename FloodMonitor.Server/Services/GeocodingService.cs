using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FloodMonitor.Server.Services;

public class GeocodingService(HttpClient http, IConfiguration configuration, ILogger<GeocodingService> logger)
{
    private readonly bool enabled = configuration.GetValue("Geocoding:Enabled", true);

    public async Task<(string? Neighborhood, string? City, string? State)> GetAddressAsync(double latitude, double longitude, CancellationToken ct)
    {
        if (!enabled)
            return (null, null, null);

        try
        {
            var url = string.Create(CultureInfo.InvariantCulture,
                $"reverse?format=jsonv2&zoom=16&accept-language=pt-BR&lat={latitude}&lon={longitude}");
            var response = await http.GetFromJsonAsync<NominatimResponse>(url, ct);
            var address = response?.Address;

            return (
                address?.Suburb ?? address?.Neighbourhood ?? address?.Quarter ?? address?.CityDistrict,
                address?.City ?? address?.Town ?? address?.Village ?? address?.Municipality,
                address?.State);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Reverse geocoding failed for {Latitude}, {Longitude}", latitude, longitude);
            return (null, null, null);
        }
    }

    private record NominatimResponse(NominatimAddress? Address);

    private record NominatimAddress(
        string? Suburb,
        string? Neighbourhood,
        string? Quarter,
        [property: JsonPropertyName("city_district")] string? CityDistrict,
        string? City,
        string? Town,
        string? Village,
        string? Municipality,
        string? State);
}
