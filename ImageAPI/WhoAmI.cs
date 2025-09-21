using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ImageAPI;

public class WhoAmI
{
    private readonly ILogger<WhoAmI> _logger;

    public WhoAmI(ILogger<WhoAmI> logger)
    {
        _logger = logger;
    }

    [Function("WhoAmI")]
    [Authorize(Policy = "RequireScope.Read")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {
        var user = req.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return new UnauthorizedResult();
        }

        string GetClaim(params string[] types)
            => types.Select(t => user.FindFirst(t)?.Value).FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? string.Empty;

        var name = GetClaim("name", "given_name");
        var preferredUsername = GetClaim("preferred_username", "upn");
        var subject = GetClaim("sub", "oid");
        var tenantId = GetClaim("tid");
        var roles = user.FindAll("roles").Select(c => c.Value).ToArray();
        var scopes = (user.FindFirst("scp")?.Value ?? string.Empty)
            .Split(' ', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);

        var claims = user.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Value).Distinct().ToArray());

        var result = new
        {
            name,
            preferredUsername,
            subject,
            tenantId,
            roles,
            scopes,
            claims
        };

        _logger.LogInformation("WhoAmI requested for subject {Subject}", subject);
        return new OkObjectResult(result);
    }
}
