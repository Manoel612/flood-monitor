using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace FloodMonitor.Server.Services;

public static class IpRateLimits
{
    public const string OccurrenceReports = "occurrence-reports";
    public const string FeedbackSubmissions = "feedback-submissions";
    public const string PageVisits = "page-visits";
    public const string AdminLogins = "admin-logins";

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(OccurrenceReports, context => FixedWindowPerIp(context, limit: 5, window: TimeSpan.FromMinutes(10)));
        options.AddPolicy(FeedbackSubmissions, context => FixedWindowPerIp(context, limit: 3, window: TimeSpan.FromMinutes(10)));
        options.AddPolicy(AdminLogins, context => FixedWindowPerIp(context, limit: 5, window: TimeSpan.FromMinutes(10)));
        options.AddPolicy(PageVisits, context => FixedWindowPerIp(context, limit: 60, window: TimeSpan.FromMinutes(1)));
    }

    private static RateLimitPartition<string> FixedWindowPerIp(HttpContext context, int limit, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, Window = window });
}
