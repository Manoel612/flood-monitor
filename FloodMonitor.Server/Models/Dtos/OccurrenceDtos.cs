using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace FloodMonitor.Server.Models.Dtos;

public record OccurrenceSummaryDto(
    int Id,
    double Latitude,
    double Longitude,
    WaterLevel WaterLevel,
    DateTime OccurredAt,
    string? Neighborhood);

public record OccurrenceDetailsDto(
    int Id,
    double Latitude,
    double Longitude,
    WaterLevel WaterLevel,
    string? Description,
    DateTime OccurredAt,
    DateTime ReportedAt,
    string? Neighborhood,
    string? City,
    string? State,
    string? PhotoUrl)
{
    public static OccurrenceDetailsDto From(Occurrence o) => new(
        o.Id, o.Latitude, o.Longitude, o.WaterLevel, o.Description,
        o.OccurredAt, o.ReportedAt, o.Neighborhood, o.City, o.State,
        o.PhotoFileName is null ? null : $"/uploads/{o.PhotoFileName}");
}

public record PageDto<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public class NewOccurrenceForm
{
    [Required(ErrorMessage = "Marque o local da enchente.")]
    [Range(-90, 90, ErrorMessage = "Latitude inválida.")]
    public double? Latitude { get; set; }

    [Required(ErrorMessage = "Marque o local da enchente.")]
    [Range(-180, 180, ErrorMessage = "Longitude inválida.")]
    public double? Longitude { get; set; }

    [Required(ErrorMessage = "Escolha a altura da água.")]
    [EnumDataType(typeof(WaterLevel), ErrorMessage = "Altura da água inválida.")]
    public WaterLevel? WaterLevel { get; set; }

    [StringLength(500, ErrorMessage = "A descrição pode ter até 500 caracteres.")]
    public string? Description { get; set; }

    public DateTime? OccurredAt { get; set; }

    public IFormFile? Photo { get; set; }

    [FromForm(Name = "site")]
    public string? HoneypotField { get; set; }

    public bool SentByBot => !string.IsNullOrEmpty(HoneypotField);
}
