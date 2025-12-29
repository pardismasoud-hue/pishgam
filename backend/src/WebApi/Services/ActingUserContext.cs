using System.Security.Claims;
using Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace WebApi.Services;

public class ActingUserContext
{
    public const string CompanyHeader = "X-Act-As-CompanyUserId";
    public const string ExpertHeader = "X-Act-As-ExpertUserId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ActingUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAdmin => _httpContextAccessor.HttpContext?.User.IsInRole(RoleNames.Admin) == true;

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? ActingCompanyUserId => GetHeaderValue(CompanyHeader);

    public string? ActingExpertUserId => GetHeaderValue(ExpertHeader);

    public string? GetEffectiveCompanyUserId()
    {
        if (IsAdmin)
        {
            return ActingCompanyUserId;
        }

        return UserId;
    }

    public string? GetEffectiveExpertUserId()
    {
        if (IsAdmin)
        {
            return ActingExpertUserId;
        }

        return UserId;
    }

    private string? GetHeaderValue(string name)
    {
        var headers = _httpContextAccessor.HttpContext?.Request.Headers;
        if (headers is null)
        {
            return null;
        }

        return headers.TryGetValue(name, out var values) ? values.ToString() : null;
    }
}
