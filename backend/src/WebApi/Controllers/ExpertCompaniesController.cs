using Domain.Constants;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Common;
using WebApi.Contracts.Companies;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("expert/companies")]
[Authorize(Roles = $"{RoleNames.Expert},{RoleNames.Admin}")]
public class ExpertCompaniesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ActingUserContext _actingUserContext;

    public ExpertCompaniesController(ApplicationDbContext dbContext, ActingUserContext actingUserContext)
    {
        _dbContext = dbContext;
        _actingUserContext = actingUserContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CompanySummaryDto>>> GetLinkedCompanies(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = _actingUserContext.GetEffectiveExpertUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Expert context is required.",
                Detail = "Select an expert to act as.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var expert = await _dbContext.ExpertProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (expert is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Expert profile not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (!expert.IsApproved)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Expert approval required.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = from link in _dbContext.CompanyExpertLinks.AsNoTracking()
                    join company in _dbContext.CompanyProfiles.AsNoTracking() on link.CompanyProfileId equals company.Id
                    join user in _dbContext.Users.AsNoTracking() on company.UserId equals user.Id
                    where link.ExpertProfileId == expert.Id
                    select new CompanySummaryDto
                    {
                        UserId = user.Id,
                        CompanyProfileId = company.Id,
                        Email = user.Email ?? string.Empty,
                        CompanyName = company.CompanyName,
                        CreatedAtUtc = company.CreatedAtUtc
                    };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.CompanyName, $"%{term}%") ||
                EF.Functions.Like(x.Email, $"%{term}%"));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.CompanyName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<CompanySummaryDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }
}
