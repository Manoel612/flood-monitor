using System.ComponentModel.DataAnnotations;

namespace FloodMonitor.Server.Models;

public enum WaterLevel
{
    Ankle = 1,
    Knee = 2,
    Waist = 3,
    AboveWaist = 4
}

public class Occurrence
{
    public int Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public WaterLevel WaterLevel { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime OccurredAt { get; set; }
    public DateTime ReportedAt { get; set; }

    [MaxLength(100)]
    public string? Neighborhood { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(100)]
    public string? PhotoFileName { get; set; }

    [MaxLength(64)]
    public string? AuthorHash { get; set; }

    public bool IsHidden { get; set; }
}
