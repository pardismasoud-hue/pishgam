using Domain.Constants;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Admin;
using WebApi.Contracts.Common;

namespace WebApi.Controllers;

[ApiController]
[Route("admin/experts")]
[Authorize(Roles = RoleNames.Admin)]
public class AdminExpertsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AdminExpertsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ExpertSummaryDto>>> GetExperts(
        [FromQuery] string? search,
        [FromQuery] bool? approved,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = from profile in _dbContext.ExpertProfiles.AsNoTracking()
                    join user in _dbContext.Users.AsNoTracking() on profile.UserId equals user.Id
                    select new ExpertSummaryDto
                    {
                        UserId = user.Id,
                        Email = user.Email ?? string.Empty,
                        FullName = profile.FullName,
                        IsApproved = profile.IsApproved,
                        CreatedAtUtc = profile.CreatedAtUtc
                    };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.Email, $"%{term}%") ||
                EF.Functions.Like(x.FullName, $"%{term}%"));
        }

        if (approved.HasValue)
        {
            query = query.Where(x => x.IsApproved == approved.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<ExpertSummaryDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveExpert([FromRoute] string id)
    {
        var profile = await _dbContext.ExpertProfiles
            .FirstOrDefaultAsync(p => p.UserId == id);

        if (profile is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Expert not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (profile.IsApproved)
        {
            return NoContent();
        }

        profile.IsApproved = true;
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
