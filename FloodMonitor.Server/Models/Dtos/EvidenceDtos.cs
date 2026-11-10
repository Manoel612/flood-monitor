using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FloodMonitor.Server.Models.Dtos;

public class NewFeedbackForm
{
    [Range(1, 5, ErrorMessage = "Escolha uma nota de 1 a 5.")]
    public int Rating { get; set; }

    [StringLength(60)]
    public string? Profile { get; set; }

    [StringLength(500, ErrorMessage = "O comentário pode ter até 500 caracteres.")]
    public string? Comment { get; set; }

    [JsonPropertyName("site")]
    public string? HoneypotField { get; set; }

    [JsonIgnore]
    public bool SentByBot => !string.IsNullOrEmpty(HoneypotField);
}

public class NewPageVisitForm
{
    [Required, StringLength(40)]
    public string Page { get; set; } = "";

    [Required, StringLength(64)]
    public string SessionId { get; set; } = "";
}

public record EvidenceSummaryDto(
    int TotalVisits,
    int UniqueSessions,
    int CommunityOccurrences,
    int TotalFeedback,
    double? AverageRating,
    IReadOnlyList<DailyActivityDto> ByDay,
    IReadOnlyList<PageTotalDto> ByPage,
    IReadOnlyList<ProfileTotalDto> FeedbackByProfile,
    IReadOnlyList<CommentDto> LatestComments);

public record DailyActivityDto(DateOnly Day, int Visits, int Sessions, int Occurrences, int Feedback);

public record PageTotalDto(string Page, int Visits);

public record ProfileTotalDto(string Profile, int Total, double AverageRating);

public record CommentDto(DateTime CreatedAt, int Rating, string? Profile, string Comment);
