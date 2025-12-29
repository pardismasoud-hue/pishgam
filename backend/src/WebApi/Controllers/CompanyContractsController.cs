using Domain.Constants;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Common;
using WebApi.Contracts.Contracts;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("company/contracts")]
[Authorize(Roles = $"{RoleNames.Company},{RoleNames.Admin}")]
public class CompanyContractsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ActingUserContext _actingUserContext;

    public CompanyContractsController(ApplicationDbContext dbContext, ActingUserContext actingUserContext)
    {
        _dbContext = dbContext;
        _actingUserContext = actingUserContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ContractDto>>> GetContracts(
        [FromQuery] bool? isActive,
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

        var query = _dbContext.Contracts
            .AsNoTracking()
            .Where(x => x.CompanyProfileId == company.Id);

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();
        var contracts = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var contractIds = contracts.Select(x => x.Id).ToArray();
        var services = await LoadContractServicesAsync(contractIds);
        var assets = await LoadContractAssetsAsync(contractIds);

        var items = contracts.Select(contract => new ContractDto
        {
            Id = contract.Id,
            CompanyProfileId = contract.CompanyProfileId,
            CompanyName = company.CompanyName,
            MonthlyPrice = contract.MonthlyPrice,
            IncludedSupportMinutesPerMonth = contract.IncludedSupportMinutesPerMonth,
            OnsiteDaysIncluded = contract.OnsiteDaysIncluded,
            IsActive = contract.IsActive,
            Services = services.TryGetValue(contract.Id, out var contractServices)
                ? contractServices
                : new List<ContractServiceDto>(),
            Assets = assets.TryGetValue(contract.Id, out var contractAssets)
                ? contractAssets
                : new List<ContractAssetDto>()
        }).ToList();

        return Ok(new PagedResult<ContractDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContractDto>> GetContract([FromRoute] Guid id)
    {
        var (company, errorResult) = await ResolveCompanyAsync(asNoTracking: true);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var contract = await _dbContext.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyProfileId == company.Id && x.Id == id);

        if (contract is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Contract not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var services = await LoadContractServicesAsync(new[] { contract.Id });
        var assets = await LoadContractAssetsAsync(new[] { contract.Id });

        return Ok(new ContractDto
        {
            Id = contract.Id,
            CompanyProfileId = contract.CompanyProfileId,
            CompanyName = company.CompanyName,
            MonthlyPrice = contract.MonthlyPrice,
            IncludedSupportMinutesPerMonth = contract.IncludedSupportMinutesPerMonth,
            OnsiteDaysIncluded = contract.OnsiteDaysIncluded,
            IsActive = contract.IsActive,
            Services = services.TryGetValue(contract.Id, out var contractServices)
                ? contractServices
                : new List<ContractServiceDto>(),
            Assets = assets.TryGetValue(contract.Id, out var contractAssets)
                ? contractAssets
                : new List<ContractAssetDto>()
        });
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

    private async Task<Dictionary<Guid, List<ContractServiceDto>>> LoadContractServicesAsync(Guid[] contractIds)
    {
        if (contractIds.Length == 0)
        {
            return new Dictionary<Guid, List<ContractServiceDto>>();
        }

        var services = await _dbContext.ContractServices
            .AsNoTracking()
            .Where(x => contractIds.Contains(x.ContractId))
            .Join(_dbContext.Services.AsNoTracking(),
                contractService => contractService.ServiceCatalogItemId,
                service => service.Id,
                (contractService, service) => new
                {
                    contractService.ContractId,
                    Service = new ContractServiceDto
                    {
                        ServiceId = service.Id,
                        ServiceName = service.Name,
                        DefaultFirstResponseMinutes = service.DefaultFirstResponseMinutes,
                        DefaultResolutionMinutes = service.DefaultResolutionMinutes,
                        CustomFirstResponseMinutes = contractService.CustomFirstResponseMinutes,
                        CustomResolutionMinutes = contractService.CustomResolutionMinutes
                    }
                })
            .ToListAsync();

        return services
            .GroupBy(x => x.ContractId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Service).ToList());
    }

    private async Task<Dictionary<Guid, List<ContractAssetDto>>> LoadContractAssetsAsync(Guid[] contractIds)
    {
        if (contractIds.Length == 0)
        {
            return new Dictionary<Guid, List<ContractAssetDto>>();
        }

        var assets = await _dbContext.ContractAssets
            .AsNoTracking()
            .Where(x => contractIds.Contains(x.ContractId))
            .Join(_dbContext.Assets.AsNoTracking(),
                contractAsset => contractAsset.AssetId,
                asset => asset.Id,
                (contractAsset, asset) => new
                {
                    contractAsset.ContractId,
                    Asset = new ContractAssetDto
                    {
                        AssetId = asset.Id,
                        AssetName = asset.Name,
                        AssetTag = asset.AssetTag,
                        SerialNumber = asset.SerialNumber
                    }
                })
            .ToListAsync();

        return assets
            .GroupBy(x => x.ContractId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Asset).ToList());
    }
}
