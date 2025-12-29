using Domain.Constants;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Assets;
using WebApi.Contracts.Common;

namespace WebApi.Controllers;

[ApiController]
[Route("admin/companies/{companyId}/assets")]
[Authorize(Roles = RoleNames.Admin)]
public class AdminAssetsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AdminAssetsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AssetDto>>> GetCompanyAssets(
        [FromRoute] string companyId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
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

    [HttpPost]
    public async Task<IActionResult> CreateAsset(
        [FromRoute] string companyId,
        [FromBody] CreateAssetRequest request)
    {
        var company = await _dbContext.CompanyProfiles
            .FirstOrDefaultAsync(x => x.UserId == companyId);

        if (company is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Company profile not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var asset = new Domain.Entities.Asset
        {
            CompanyProfileId = company.Id,
            Name = request.Name.Trim(),
            AssetTag = request.AssetTag?.Trim(),
            SerialNumber = request.SerialNumber?.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "admin"
        };

        _dbContext.Assets.Add(asset);
        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AssetDto>> UpdateAsset(
        [FromRoute] string companyId,
        [FromRoute] Guid id,
        [FromBody] UpdateAssetRequest request)
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

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyProfileId == company.Id);

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
        asset.UpdatedAtUtc = DateTime.UtcNow;
        asset.UpdatedBy = User.Identity?.Name ?? "admin";

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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsset(
        [FromRoute] string companyId,
        [FromRoute] Guid id)
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

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyProfileId == company.Id);

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
}
