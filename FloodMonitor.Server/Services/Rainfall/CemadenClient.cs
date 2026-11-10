using System.Globalization;
using System.Net;
using System.Text.Json;

namespace FloodMonitor.Server.Services.Rainfall;

public record CemadenStation(string Code, string Name, double Latitude, double Longitude);

public record CemadenDailyTotal(string StationCode, double Millimeters);

public class CemadenClient
{
    public const string HttpClientName = "cemaden";

    private const string TokenUrl = "https://ped.cemaden.gov.br/SGAA/rest/controle-token/tokens";
    private const string DataUrl = "https://sws.cemaden.gov.br/PED/rest/";
    private const string RainGaugeStationType = "1";
    private const int MaxRequestsPerMinute = 10;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(3);

    private readonly IHttpClientFactory httpClientFactory;
    private readonly string? email;
    private readonly string? password;
    private readonly SemaphoreSlim requestLock = new(1, 1);
    private readonly Queue<DateTime> recentRequests = new();
    private string? token;
    private DateTime tokenExpiresAt;

    public CemadenClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        this.httpClientFactory = httpClientFactory;
        email = configuration["Cemaden:Email"]?.Trim();
        password = configuration["Cemaden:Password"];
    }

    public bool IsConfigured => !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password);

    public async Task<List<CemadenStation>> GetRainGaugesAsync(int ibgeCode, CancellationToken ct)
    {
        var root = await GetJsonAsync(
            $"pcds-cadastro/dados-cadastrais?codibge={ibgeCode}&tipoestacao={RainGaugeStationType}&formato=JSON", ct);

        var stations = new List<CemadenStation>();
        foreach (var item in Items(root))
        {
            var code = Text(item, "codestacao");
            var latitude = Number(item, "latitude");
            var longitude = Number(item, "longitude");
            if (code is not null && latitude is not null && longitude is not null)
                stations.Add(new CemadenStation(code, Text(item, "nome") ?? code, latitude.Value, longitude.Value));
        }
        return stations;
    }

    public async Task<List<CemadenDailyTotal>> GetLast24HourTotalsAsync(int ibgeCode, DateTime referenceUtc, CancellationToken ct)
    {
        var reference = referenceUtc.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);
        var root = await GetJsonAsync($"pcds-acum/acumulados-historicos?codibge={ibgeCode}&data={reference}&formato=JSON", ct);

        var totals = new List<CemadenDailyTotal>();
        foreach (var item in Items(root))
        {
            var code = Text(item, "codestacao");
            var millimeters = Number(item, "acc24hr");
            if (code is not null && millimeters is >= 0)
                totals.Add(new CemadenDailyTotal(code, Math.Round(millimeters.Value, 1)));
        }
        return totals;
    }

    private async Task<JsonElement> GetJsonAsync(string pathAndQuery, CancellationToken ct)
    {
        await requestLock.WaitAsync(ct);
        try
        {
            using var response = await SendDataRequestAsync(pathAndQuery, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                token = null;
                using var retry = await SendDataRequestAsync(pathAndQuery, ct);
                return await ReadJsonAsync(retry, ct);
            }
            return await ReadJsonAsync(response, ct);
        }
        finally
        {
            requestLock.Release();
        }
    }

    private async Task<HttpResponseMessage> SendDataRequestAsync(string pathAndQuery, CancellationToken ct)
    {
        var currentToken = await GetTokenAsync(ct);
        await WaitForRateLimitAsync(ct);

        using var request = new HttpRequestMessage(HttpMethod.Get, DataUrl + pathAndQuery);
        request.Headers.Add("token", currentToken);
        return await httpClientFactory.CreateClient(HttpClientName).SendAsync(request, ct);
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (token is not null && DateTime.UtcNow < tokenExpiresAt)
            return token;

        if (!IsConfigured)
            throw new InvalidOperationException("Cemaden:Email and Cemaden:Password are not set.");

        await WaitForRateLimitAsync(ct);
        using var response = await httpClientFactory.CreateClient(HttpClientName)
            .PostAsJsonAsync(TokenUrl, new { email, password }, ct);
        var root = await ReadJsonAsync(response, ct);

        var newToken = root.ValueKind == JsonValueKind.String ? root.GetString() : Text(root, "token");
        if (string.IsNullOrEmpty(newToken))
            throw new InvalidOperationException("The CEMADEN login did not return a token.");

        token = newToken;
        tokenExpiresAt = DateTime.UtcNow + TokenLifetime;
        return token;
    }

    private async Task WaitForRateLimitAsync(CancellationToken ct)
    {
        while (recentRequests.Count > 0 && DateTime.UtcNow - recentRequests.Peek() > TimeSpan.FromMinutes(1))
            recentRequests.Dequeue();

        if (recentRequests.Count >= MaxRequestsPerMinute)
        {
            var wait = recentRequests.Peek() + TimeSpan.FromMinutes(1) - DateTime.UtcNow;
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, ct);
            recentRequests.Dequeue();
        }

        recentRequests.Enqueue(DateTime.UtcNow);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"CEMADEN answered {(int)response.StatusCode}.", null, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return document.RootElement.Clone();
    }

    private static IEnumerable<JsonElement> Items(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return root.EnumerateArray();
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            return data.EnumerateArray();
        return [];
    }

    private static string? Text(JsonElement item, string name)
    {
        if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty(name, out var value))
            return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static double? Number(JsonElement item, string name)
    {
        var text = Text(item, name);
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : null;
    }
}
