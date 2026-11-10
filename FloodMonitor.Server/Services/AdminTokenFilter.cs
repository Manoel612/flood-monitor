using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FloodMonitor.Server.Services;

public class AdminTokenFilter(AdminAccess adminAccess) : IAuthorizationFilter
{
    private const string BearerPrefix = "Bearer ";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!adminAccess.IsConfigured)
        {
            context.Result = new NotFoundResult();
            return;
        }

        var header = context.HttpContext.Request.Headers.Authorization.ToString();
        var token = header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase) ? header[BearerPrefix.Length..] : null;

        if (!adminAccess.IsValidToken(token))
            context.Result = new UnauthorizedResult();
    }
}
