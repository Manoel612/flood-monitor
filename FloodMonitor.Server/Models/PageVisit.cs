using System.ComponentModel.DataAnnotations;

namespace FloodMonitor.Server.Models;

public class PageVisit
{
    public long Id { get; set; }

    [MaxLength(40)]
    public required string Page { get; set; }

    [MaxLength(64)]
    public required string SessionId { get; set; }

    public DateTime VisitedAt { get; set; }
}
