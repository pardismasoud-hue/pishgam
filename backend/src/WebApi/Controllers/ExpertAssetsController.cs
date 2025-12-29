using Domain.Constants;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Assets;
using WebApi.Contracts.Common;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("expert/assets")]
[Authorize(Roles = $"{RoleNames.Expert},{RoleNames.Admin}")]
public class ExpertAssetsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ActingUserContext _actingUserContext;

    public ExpertAssetsController(ApplicationDbContext dbContext, ActingUserContext actingUserContext)
    {
        _dbContext = dbContext;
        _actingUserContext = actingUserContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AssetDto>>> GetAssets(
        [FromQuery] string? companyId,
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

        Guid[] companyProfileIds;
        if (!string.IsNullOrWhiteSpace(companyId))
        {
            var company = await _dbContext.CompanyProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == companyId);

            if (company is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Company profile not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var linked = await _dbContext.CompanyExpertLinks
                .AsNoTracking()
                .AnyAsync(x => x.CompanyProfileId == company.Id && x.ExpertProfileId == expert.Id);

            if (!linked)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Expert is not linked to this company.",
                    Status = StatusCodes.Status403Forbidden
                });
            }

            companyProfileIds = new[] { company.Id };
        }
        else
        {
            companyProfileIds = await _dbContext.CompanyExpertLinks
                .AsNoTracking()
                .Where(x => x.ExpertProfileId == expert.Id)
                .Select(x => x.CompanyProfileId)
                .Distinct()
                .ToArrayAsync();
        }

        if (companyProfileIds.Length == 0)
        {
            return Ok(new PagedResult<AssetDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                Items = Array.Empty<AssetDto>()
            });
        }

        var assetsQuery = _dbContext.Assets
            .AsNoTracking()
            .Where(x => companyProfileIds.Contains(x.CompanyProfileId));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            assetsQuery = assetsQuery.Where(x =>
                EF.Functions.Like(x.Name, $"%{term}%") ||
                (x.AssetTag != null && EF.Functions.Like(x.AssetTag, $"%{term}%")) ||
                (x.SerialNumber != null && EF.Functions.Like(x.SerialNumber, $"%{term}%")));
        }

        var totalCount = await assetsQuery.CountAsync();
        var items = await assetsQuery
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(_dbContext.CompanyProfiles.AsNoTracking(),
                asset => asset.CompanyProfileId,
                company => company.Id,
                (asset, company) => new AssetDto
                {
                    Id = asset.Id,
                    CompanyProfileId = company.Id,
                    CompanyName = company.CompanyName,
                    Name = asset.Name,
                    AssetTag = asset.AssetTag,
                    SerialNumber = asset.SerialNumber,
                    Notes = asset.Notes
                })
            .ToListAsync();

        return Ok(new PagedResult<AssetDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }
}
