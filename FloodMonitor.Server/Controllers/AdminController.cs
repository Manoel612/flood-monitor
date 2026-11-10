using FloodMonitor.Server.Models.Dtos;
using FloodMonitor.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FloodMonitor.Server.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController(AdminAccess adminAccess, ILogger<AdminController> logger) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting(IpRateLimits.AdminLogins)]
    public ActionResult<AdminLoginResultDto> Login(AdminLoginForm form)
    {
        if (!adminAccess.IsConfigured)
            return NotFound();

        if (!adminAccess.AreValidCredentials(form.Username, form.Password))
        {
            logger.LogWarning("Failed admin login from {RemoteIp}", HttpContext.Connection.RemoteIpAddress);
            return Unauthorized();
        }

        return new AdminLoginResultDto(adminAccess.Token!);
    }
}
