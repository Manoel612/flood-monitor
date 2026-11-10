using System.Text;
using FloodMonitor.Server.Data;
using FloodMonitor.Server.Models;
using FloodMonitor.Server.Models.Dtos;
using FloodMonitor.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FloodMonitor.Server.Controllers;

[ApiController]
[Route("api/evidence")]
public class EvidenceController(AppDbContext db, TimeZoneInfo timeZone) : ControllerBase
{
    [HttpPost("visits")]
    [EnableRateLimiting(IpRateLimits.PageVisits)]
    public async Task<IActionResult> RecordVisit(NewPageVisitForm form, CancellationToken ct)
    {
        db.PageVisits.Add(new PageVisit { Page = form.Page, SessionId = form.SessionId, VisitedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("summary")]
    [ServiceFilter(typeof(AdminTokenFilter))]
    public async Task<EvidenceSummaryDto> Summary(CancellationToken ct)
    {
        var visits = await db.PageVisits.AsNoTracking()
            .Select(v => new { v.Page, v.SessionId, v.VisitedAt })
            .ToListAsync(ct);
        var reportDates = await db.Occurrences.AsNoTracking()
            .Select(o => o.ReportedAt)
            .ToListAsync(ct);
        var feedback = await db.Feedback.AsNoTracking()
            .OrderByDescending(f => f.Id)
            .ToListAsync(ct);

        var visitsByDay = visits
            .GroupBy(v => LocalDay(v.VisitedAt))
            .ToDictionary(g => g.Key, g => (Visits: g.Count(), Sessions: g.Select(v => v.SessionId).Distinct().Count()));
        var occurrencesByDay = reportDates.GroupBy(LocalDay).ToDictionary(g => g.Key, g => g.Count());
        var feedbackByDay = feedback.GroupBy(f => LocalDay(f.CreatedAt)).ToDictionary(g => g.Key, g => g.Count());

        var byDay = visitsByDay.Keys
            .Union(occurrencesByDay.Keys)
            .Union(feedbackByDay.Keys)
            .OrderByDescending(day => day)
            .Select(day => new DailyActivityDto(
                day,
                visitsByDay.GetValueOrDefault(day).Visits,
                visitsByDay.GetValueOrDefault(day).Sessions,
                occurrencesByDay.GetValueOrDefault(day),
                feedbackByDay.GetValueOrDefault(day)))
            .ToList();

        var byPage = visits
            .GroupBy(v => v.Page)
            .Select(g => new PageTotalDto(g.Key, g.Count()))
            .OrderByDescending(p => p.Visits)
            .ToList();

        var byProfile = feedback
            .GroupBy(f => f.Profile ?? "Não informado")
            .Select(g => new ProfileTotalDto(g.Key, g.Count(), Math.Round(g.Average(f => f.Rating), 1)))
            .OrderByDescending(p => p.Total)
            .ToList();

        var latestComments = feedback
            .Where(f => f.Comment is not null)
            .Take(20)
            .Select(f => new CommentDto(f.CreatedAt, f.Rating, f.Profile, f.Comment!))
            .ToList();

        return new EvidenceSummaryDto(
            visits.Count,
            visits.Select(v => v.SessionId).Distinct().Count(),
            reportDates.Count,
            feedback.Count,
            feedback.Count > 0 ? Math.Round(feedback.Average(f => f.Rating), 1) : null,
            byDay,
            byPage,
            byProfile,
            latestComments);
    }

    [HttpGet("visits.csv")]
    [ServiceFilter(typeof(AdminTokenFilter))]
    public async Task<IActionResult> VisitsCsv(CancellationToken ct)
    {
        var visits = await db.PageVisits.AsNoTracking().OrderBy(v => v.Id).ToListAsync(ct);
        var csv = new StringBuilder("data;hora;pagina;sessao\n");
        foreach (var v in visits)
        {
            var local = ToLocalTime(v.VisitedAt);
            csv.Append($"{local:dd/MM/yyyy};{local:HH:mm:ss};{CsvFieldWithoutFormula(v.Page)};{CsvFieldWithoutFormula(v.SessionId)}\n");
        }
        return CsvFile(csv, "acessos.csv");
    }

    [HttpGet("feedback.csv")]
    [ServiceFilter(typeof(AdminTokenFilter))]
    public async Task<IActionResult> FeedbackCsv(CancellationToken ct)
    {
        var feedback = await db.Feedback.AsNoTracking().OrderBy(f => f.Id).ToListAsync(ct);
        var csv = new StringBuilder("data;hora;nota;perfil;comentario\n");
        foreach (var f in feedback)
        {
            var local = ToLocalTime(f.CreatedAt);
            csv.Append($"{local:dd/MM/yyyy};{local:HH:mm:ss};{f.Rating};{CsvFieldWithoutFormula(f.Profile)};{CsvFieldWithoutFormula(f.Comment)}\n");
        }
        return CsvFile(csv, "avaliacoes.csv");
    }

    [HttpGet("occurrences.csv")]
    [ServiceFilter(typeof(AdminTokenFilter))]
    public async Task<IActionResult> OccurrencesCsv(CancellationToken ct)
    {
        var occurrences = await db.Occurrences.AsNoTracking()
            .OrderBy(o => o.Id)
            .ToListAsync(ct);
        var csv = new StringBuilder("id;registrado_em;ocorreu_em;nivel;bairro;cidade;estado;latitude;longitude;tem_foto;oculto\n");
        foreach (var o in occurrences)
        {
            csv.Append($"{o.Id};{ToLocalTime(o.ReportedAt):dd/MM/yyyy HH:mm};{ToLocalTime(o.OccurredAt):dd/MM/yyyy HH:mm};{o.WaterLevel};");
            csv.Append($"{CsvFieldWithoutFormula(o.Neighborhood)};{CsvFieldWithoutFormula(o.City)};{CsvFieldWithoutFormula(o.State)};{o.Latitude};{o.Longitude};");
            csv.Append($"{(o.PhotoFileName is null ? "não" : "sim")};{(o.IsHidden ? "sim" : "não")}\n");
        }
        return CsvFile(csv, "ocorrencias.csv");
    }

    private DateTime ToLocalTime(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);

    private DateOnly LocalDay(DateTime utc) => DateOnly.FromDateTime(ToLocalTime(utc));

    private static string CsvFieldWithoutFormula(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        if (text[0] is '=' or '+' or '-' or '@')
            text = "'" + text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    private FileContentResult CsvFile(StringBuilder csv, string fileName) =>
        File([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(csv.ToString())], "text/csv; charset=utf-8", fileName);
}
