using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FloodMonitor.Server.Services.Rainfall;

public class IbgeMunicipalities(IHttpClientFactory httpClientFactory)
{
    public const string HttpClientName = "ibge";

    private readonly SemaphoreSlim loadLock = new(1, 1);
    private Dictionary<string, int>? codesByCityAndState;

    public async Task<int?> FindCodeAsync(string city, string state, CancellationToken ct)
    {
        var codes = await LoadAsync(ct);
        return codes.TryGetValue(Key(city, state), out var code) ? code : null;
    }

    private async Task<Dictionary<string, int>> LoadAsync(CancellationToken ct)
    {
        if (codesByCityAndState is not null)
            return codesByCityAndState;

        await loadLock.WaitAsync(ct);
        try
        {
            if (codesByCityAndState is null)
            {
                var municipalities = await httpClientFactory.CreateClient(HttpClientName)
                    .GetFromJsonAsync<List<JsonElement>>("api/v1/localidades/municipios?view=nivelado", ct) ?? [];

                var codes = new Dictionary<string, int>();
                foreach (var municipality in municipalities)
                {
                    var code = municipality.GetProperty("municipio-id").GetInt32();
                    var name = municipality.GetProperty("municipio-nome").GetString() ?? "";
                    var stateName = municipality.GetProperty("UF-nome").GetString() ?? "";
                    codes.TryAdd(Key(name, stateName), code);
                }

                codesByCityAndState = codes;
            }

            return codesByCityAndState;
        }
        finally
        {
            loadLock.Release();
        }
    }

    private static string Key(string city, string state) => $"{Normalize(city)}|{Normalize(state)}";

    private static string Normalize(string text)
    {
        var withoutAccents = new StringBuilder();
        foreach (var character in text.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                withoutAccents.Append(character);
        }
        return withoutAccents.ToString().ToLowerInvariant();
    }
}
