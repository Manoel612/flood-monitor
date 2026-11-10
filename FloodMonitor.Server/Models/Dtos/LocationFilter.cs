using System.ComponentModel.DataAnnotations;

namespace FloodMonitor.Server.Models.Dtos;

public class LocationFilter
{
    public const int DefaultRadiusInMeters = 1000;

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? Neighborhood { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude inválida.")]
    public double? Lat { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude inválida.")]
    public double? Lon { get; set; }

    [Range(100, 5000, ErrorMessage = "O raio deve ficar entre 100 m e 5 km.")]
    public int? Radius { get; set; }

    public bool ByRadius => Lat is not null && Lon is not null;

    public int RadiusInMeters => Radius ?? DefaultRadiusInMeters;
}

public record PlaceTotalDto(string? State, string? City, string? Neighborhood, int Total);

public record PlacesDto(
    IReadOnlyList<PlaceTotalDto> States,
    IReadOnlyList<PlaceTotalDto> Cities,
    IReadOnlyList<PlaceTotalDto> Neighborhoods);
