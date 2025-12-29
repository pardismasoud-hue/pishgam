using Domain.Constants;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Assets;
using WebApi.Contracts.Common;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("company/assets")]
[Authorize(Roles = $"{RoleNames.Company},{RoleNames.Admin}")]
public class CompanyAssetsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ActingUserContext _actingUserContext;

    public CompanyAssetsController(ApplicationDbContext dbContext, ActingUserContext actingUserContext)
    {
        _dbContext = dbContext;
        _actingUserContext = actingUserContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AssetDto>>> GetAssets(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (company, errorResult) = await ResolveCompanyAsync(asNoTracking: true);
        if (errorResult is not null)
        {
            return errorResult;
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Assets
            .AsNoTracking()
            .Where(x => x.CompanyProfileId == company.Id);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.Name, $"%{term}%") ||
                (x.AssetTag != null && EF.Functions.Like(x.AssetTag, $"%{term}%")) ||
                (x.SerialNumber != null && EF.Functions.Like(x.SerialNumber, $"%{term}%")));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AssetDto
            {
                Id = x.Id,
                CompanyProfileId = company.Id,
                CompanyName = company.CompanyName,
                Name = x.Name,
                AssetTag = x.AssetTag,
                SerialNumber = x.SerialNumber,
                Notes = x.Notes
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssetDto>> GetAsset([FromRoute] Guid id)
    {
        var (company, errorResult) = await ResolveCompanyAsync(asNoTracking: true);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var asset = await _dbContext.Assets
            .AsNoTracking()
            .Where(x => x.CompanyProfileId == company.Id && x.Id == id)
            .Select(x => new AssetDto
            {
                Id = x.Id,
                CompanyProfileId = company.Id,
                CompanyName = company.CompanyName,
                Name = x.Name,
                AssetTag = x.AssetTag,
                SerialNumber = x.SerialNumber,
                Notes = x.Notes
            })
            .FirstOrDefaultAsync();

        if (asset is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Asset not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(asset);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsset([FromBody] CreateAssetRequest request)
    {
        var (company, errorResult) = await ResolveCompanyAsync(asNoTracking: false);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var asset = new Asset
        {
            CompanyProfileId = company.Id,
            Name = request.Name.Trim(),
            AssetTag = request.AssetTag?.Trim(),
            SerialNumber = request.SerialNumber?.Trim(),
            Notes = request.Notes?.Trim()
        };

        _dbContext.Assets.Add(asset);
        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsset([FromRoute] Guid id, [FromBody] UpdateAssetRequest request)
    {
        var (company, errorResult) = await ResolveCompanyAsync(asNoTracking: false);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.CompanyProfileId == company.Id && x.Id == id);

        if (asset is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Asset not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        asset.Name = request.Name.Trim();
        asset.AssetTag = request.AssetTag?.Trim();
        asset.SerialNumber = request.SerialNumber?.Trim();
        asset.Notes = request.Notes?.Trim();

        await _dbContext.SaveChangesAsync();

        return Ok(new AssetDto
        {
            Id = asset.Id,
            CompanyProfileId = company.Id,
            CompanyName = company.CompanyName,
            Name = asset.Name,
            AssetTag = asset.AssetTag,
            SerialNumber = asset.SerialNumber,
            Notes = asset.Notes
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsset([FromRoute] Guid id)
    {
        var (company, errorResult) = await ResolveCompanyAsync(asNoTracking: false);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.CompanyProfileId == company.Id && x.Id == id);

        if (asset is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Asset not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        _dbContext.Assets.Remove(asset);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private async Task<(CompanyProfile? Company, ActionResult? ErrorResult)> ResolveCompanyAsync(bool asNoTracking)
    {
        var userId = _actingUserContext.GetEffectiveCompanyUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return (null, BadRequest(new ProblemDetails
            {
                Title = "Company context is required.",
                Detail = "Select a company to act as.",
                Status = StatusCodes.Status400BadRequest
            }));
        }

        var query = _dbContext.CompanyProfiles.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var company = await query.FirstOrDefaultAsync(x => x.UserId == userId);
        if (company is null)
        {
            return (null, NotFound(new ProblemDetails
            {
                Title = "Company profile not found.",
                Status = StatusCodes.Status404NotFound
            }));
        }

        return (company, null);
    }
}
