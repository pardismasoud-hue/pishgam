using Domain.Constants;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Common;
using WebApi.Contracts.Companies;

namespace WebApi.Controllers;

[ApiController]
[Route("admin/companies")]
[Authorize(Roles = RoleNames.Admin)]
public class AdminCompaniesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AdminCompaniesController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CompanySummaryDto>>> GetCompanies(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = from profile in _dbContext.CompanyProfiles.AsNoTracking()
                    join user in _dbContext.Users.AsNoTracking() on profile.UserId equals user.Id
                    select new CompanySummaryDto
                    {
                        UserId = user.Id,
                        CompanyProfileId = profile.Id,
                        Email = user.Email ?? string.Empty,
                        CompanyName = profile.CompanyName,
                        CreatedAtUtc = profile.CreatedAtUtc
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
