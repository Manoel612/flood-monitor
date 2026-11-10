using System.ComponentModel.DataAnnotations;

namespace FloodMonitor.Server.Models;

public enum RainfallSource
{
    OpenMeteo,
    Cemaden
}

public class RainfallReading
{
    public const string NoStation = "";

    public int Id { get; set; }
    public RainfallSource Source { get; set; }

    [MaxLength(100)]
    public required string City { get; set; }

    [MaxLength(100)]
    public required string State { get; set; }

    [MaxLength(40)]
    public string StationCode { get; set; } = NoStation;

    [MaxLength(150)]
    public string? StationName { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateOnly Day { get; set; }
    public double PrecipitationMm { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RainfallCheckedDay
{
    public int Id { get; set; }
    public RainfallSource Source { get; set; }

    [MaxLength(100)]
    public required string City { get; set; }

    [MaxLength(100)]
    public required string State { get; set; }

    public DateOnly Day { get; set; }
}
