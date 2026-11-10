using FloodMonitor.Server.Data;
using FloodMonitor.Server.Models;
using FloodMonitor.Server.Models.Dtos;
using FloodMonitor.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FloodMonitor.Server.Controllers;

[ApiController]
[Route("api/feedback")]
public class FeedbackController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(IpRateLimits.FeedbackSubmissions)]
    public async Task<IActionResult> Submit(NewFeedbackForm form, CancellationToken ct)
    {
        if (form.SentByBot)
            return BadRequest();

        db.Feedback.Add(new Feedback
        {
            Rating = form.Rating,
            Profile = TextHelpers.TrimOrNull(form.Profile),
            Comment = TextHelpers.TrimOrNull(form.Comment),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}
