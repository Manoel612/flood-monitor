using FloodMonitor.Server.Data;
using FloodMonitor.Server.Models;
using FloodMonitor.Server.Models.Dtos;
using FloodMonitor.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FloodMonitor.Server.Controllers;

[ApiController]
[Route("api/occurrences")]
public class OccurrencesController(
    AppDbContext db,
    PhotoStorage photos,
    GeocodingService geocoding) : ControllerBase
{
    private const long MaxRequestSize = PhotoStorage.MaxSizeInBytes + 1024 * 1024;
    private const int MaxPageSize = 50;
    private static readonly TimeSpan ClockSkewTolerance = TimeSpan.FromMinutes(10);

    [HttpGet]
    public async Task<List<OccurrenceSummaryDto>> List(DateTime? from, DateTime? to, CancellationToken ct) =>
        await db.Occurrences.AsNoTracking()
            .VisibleInPeriod(from, to)
            .OrderByDescending(o => o.OccurredAt)
            .Select(o => new OccurrenceSummaryDto(o.Id, o.Latitude, o.Longitude, o.WaterLevel, o.OccurredAt, o.Neighborhood))
            .ToListAsync(ct);

    [HttpGet("recent")]
    public async Task<PageDto<OccurrenceDetailsDto>> Recent(
        DateTime? from,
        DateTime? to,
        [FromQuery] LocationFilter location,
        [FromHeader(Name = Authorship.Header)] string? authorId,
        bool mine = false,
        int page = 1,
        int pageSize = 12,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = db.Occurrences.AsNoTracking().VisibleInPeriod(from, to).InLocation(location);

        if (mine)
        {
            var authorHash = Authorship.Hash(authorId);
            if (authorHash is null)
                return new PageDto<OccurrenceDetailsDto>([], 0, page, pageSize);

            query = query.ByAuthor(authorHash);
        }

        var matchingIds = await NewestIdsWithinRadiusAsync(query, location, ct);
        var pageIds = matchingIds.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var items = await DetailsInOrderAsync(pageIds, ct);

        return new PageDto<OccurrenceDetailsDto>(items, matchingIds.Count, page, pageSize);
    }

    [HttpGet("places")]
    public async Task<PlacesDto> Places(CancellationToken ct)
    {
        var addresses = await db.Occurrences.AsNoTracking()
            .VisibleInPeriod(null, null)
            .Select(o => new { o.State, o.City, o.Neighborhood })
            .ToListAsync(ct);

        var alphabetical = TextHelpers.PortugueseAlphabeticalOrder;

        var states = addresses
            .Where(a => a.State is not null)
            .GroupBy(a => a.State)
            .Select(group => new PlaceTotalDto(group.Key, null, null, group.Count()))
            .OrderBy(p => p.State, alphabetical)
            .ToList();

        var cities = addresses
            .Where(a => a.City is not null)
            .GroupBy(a => (a.City, a.State))
            .Select(group => new PlaceTotalDto(group.Key.State, group.Key.City, null, group.Count()))
            .OrderBy(p => p.City, alphabetical)
            .ThenBy(p => p.State, alphabetical)
            .ToList();

        var neighborhoods = addresses
            .Where(a => a.Neighborhood is not null)
            .GroupBy(a => (a.Neighborhood, a.City, a.State))
            .Select(group => new PlaceTotalDto(group.Key.State, group.Key.City, group.Key.Neighborhood, group.Count()))
            .OrderBy(p => p.Neighborhood, alphabetical)
            .ThenBy(p => p.City, alphabetical)
            .ToList();

        return new PlacesDto(states, cities, neighborhoods);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OccurrenceDetailsDto>> Get(int id, CancellationToken ct)
    {
        var occurrence = await db.Occurrences.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id && !o.IsHidden, ct);
        if (occurrence is null)
            return NotFound();

        return OccurrenceDetailsDto.From(occurrence);
    }

    [HttpPost]
    [EnableRateLimiting(IpRateLimits.OccurrenceReports)]
    [RequestSizeLimit(MaxRequestSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestSize)]
    public async Task<ActionResult<OccurrenceDetailsDto>> Report(
        [FromForm] NewOccurrenceForm form,
        [FromHeader(Name = Authorship.Header)] string? authorId,
        CancellationToken ct)
    {
        if (form.SentByBot)
            return BadRequest();

        var now = DateTime.UtcNow;
        var occurredAt = form.OccurredAt?.ToUniversalTime() ?? now;
        if (occurredAt > now + ClockSkewTolerance)
        {
            ModelState.AddModelError(nameof(form.OccurredAt), "A data da ocorrência não pode estar no futuro.");
            return ValidationProblem(ModelState);
        }

        string? savedPhoto = null;
        if (form.Photo is not null)
        {
            var (fileName, error) = await photos.SaveAsync(form.Photo, ct);
            if (error is not null)
            {
                ModelState.AddModelError(nameof(form.Photo), error);
                return ValidationProblem(ModelState);
            }
            savedPhoto = fileName;
        }

        var latitude = form.Latitude!.Value;
        var longitude = form.Longitude!.Value;
        var (neighborhood, city, state) = await geocoding.GetAddressAsync(latitude, longitude, ct);

        var occurrence = new Occurrence
        {
            Latitude = latitude,
            Longitude = longitude,
            WaterLevel = form.WaterLevel!.Value,
            Description = TextHelpers.TrimOrNull(form.Description),
            OccurredAt = occurredAt,
            ReportedAt = now,
            Neighborhood = neighborhood,
            City = city,
            State = state,
            PhotoFileName = savedPhoto,
            AuthorHash = Authorship.Hash(authorId)
        };

        db.Occurrences.Add(occurrence);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = occurrence.Id }, OccurrenceDetailsDto.From(occurrence));
    }

    [HttpDelete("{id:int}")]
    [ServiceFilter(typeof(AdminTokenFilter))]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var occurrence = await db.Occurrences.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (occurrence is null)
            return NotFound();

        db.Occurrences.Remove(occurrence);
        await db.SaveChangesAsync(ct);

        if (occurrence.PhotoFileName is not null)
            photos.Delete(occurrence.PhotoFileName);

        return NoContent();
    }

    private static async Task<List<int>> NewestIdsWithinRadiusAsync(
        IQueryable<Occurrence> query, LocationFilter location, CancellationToken ct)
    {
        var positions = await query
            .OrderByDescending(o => o.OccurredAt)
            .ThenByDescending(o => o.Id)
            .Select(o => new { o.Id, o.Latitude, o.Longitude })
            .ToListAsync(ct);

        return positions
            .Where(p => location.IsWithinRadius(p.Latitude, p.Longitude))
            .Select(p => p.Id)
            .ToList();
    }

    private async Task<List<OccurrenceDetailsDto>> DetailsInOrderAsync(List<int> ids, CancellationToken ct)
    {
        var occurrencesById = await db.Occurrences.AsNoTracking()
            .Where(o => ids.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, ct);

        return ids.Select(id => OccurrenceDetailsDto.From(occurrencesById[id])).ToList();
    }
}
