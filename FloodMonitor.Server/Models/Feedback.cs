using System.ComponentModel.DataAnnotations;

namespace FloodMonitor.Server.Models;

public class Feedback
{
    public int Id { get; set; }

    public int Rating { get; set; }

    [MaxLength(60)]
    public string? Profile { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}
