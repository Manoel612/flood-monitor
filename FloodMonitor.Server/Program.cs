using System.Globalization;
using System.Text.Json.Serialization;
using FloodMonitor.Server.Data;
using FloodMonitor.Server.Services;
using FloodMonitor.Server.Services.Rainfall;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.HttpOverrides;

UseDotAsDecimalSeparator();

var builder = WebApplication.CreateBuilder(args);

var dataFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataFolder);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(dataFolder, "flood-monitor.db")}"));

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

const string userAgent = "FloodMonitor/0.1 (UNINTER academic project)";

builder.Services.AddSingleton<PhotoStorage>();
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    o.ForwardedForHeaderName = "X-Cliente-IP";   // header que o Caddy coloca (passo 5)
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services.AddHttpClient<GeocodingService>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    client.Timeout = TimeSpan.FromSeconds(5);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
});

builder.Services.AddHttpClient(OpenMeteoClient.HttpClientName, client =>
{
    client.BaseAddress = new Uri("https://archive-api.open-meteo.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
});
builder.Services.AddHttpClient(IbgeMunicipalities.HttpClientName, client =>
{
    client.BaseAddress = new Uri("https://servicodados.ibge.gov.br/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
});
builder.Services.AddHttpClient(CemadenClient.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
});
builder.Services.AddSingleton<OpenMeteoClient>();
builder.Services.AddSingleton<IbgeMunicipalities>();
builder.Services.AddSingleton<CemadenClient>();
builder.Services.AddSingleton<RainfallSyncService>();
builder.Services.AddHostedService(services => services.GetRequiredService<RainfallSyncService>());

builder.Services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById(
    builder.Configuration["TimeZone"] ?? "America/Sao_Paulo"));

builder.Services.AddRateLimiter(IpRateLimits.Configure);
builder.Services.AddSingleton<AdminAccess>();
builder.Services.AddSingleton<AdminTokenFilter>();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DatabaseSchema.CreateDatabaseOrMissingTables(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseSecurityHeaders();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(app.Services.GetRequiredService<PhotoStorage>().Folder),
    RequestPath = "/uploads"
});

app.UseRateLimiter();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

static void UseDotAsDecimalSeparator()
{
    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
}
