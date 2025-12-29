using Domain.Constants;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using WebApi.Contracts.Common;
using WebApi.Contracts.Contracts;

namespace WebApi.Controllers;

[ApiController]
[Route("admin/contracts")]
[Authorize(Roles = RoleNames.Admin)]
public class AdminContractsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AdminContractsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ContractDto>>> GetContracts(
        [FromQuery] string? companyId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Contracts.AsNoTracking();

        CompanyProfile? company = null;
        if (!string.IsNullOrWhiteSpace(companyId))
        {
            company = await _dbContext.CompanyProfiles
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

            query = query.Where(x => x.CompanyProfileId == company.Id);
        }

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
        var companyIds = contracts.Select(x => x.CompanyProfileId).Distinct().ToArray();
        var companies = await LoadCompanyNamesAsync(companyIds);

        var services = await LoadContractServicesAsync(contractIds);
        var assets = await LoadContractAssetsAsync(contractIds);

        var items = contracts.Select(contract => new ContractDto
        {
            Id = contract.Id,
            CompanyProfileId = contract.CompanyProfileId,
            CompanyName = companies.TryGetValue(contract.CompanyProfileId, out var name) ? name : string.Empty,
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
        var contract = await _dbContext.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (contract is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Contract not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var companyName = await _dbContext.CompanyProfiles
            .AsNoTracking()
            .Where(x => x.Id == contract.CompanyProfileId)
            .Select(x => x.CompanyName)
            .FirstOrDefaultAsync() ?? string.Empty;

        var services = await LoadContractServicesAsync(new[] { contract.Id });
        var assets = await LoadContractAssetsAsync(new[] { contract.Id });

        return Ok(new ContractDto
        {
            Id = contract.Id,
            CompanyProfileId = contract.CompanyProfileId,
            CompanyName = companyName,
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

    [HttpPost]
    public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request)
    {
        var company = await _dbContext.CompanyProfiles
            .FirstOrDefaultAsync(x => x.UserId == request.CompanyUserId);

        if (company is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Company profile not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (request.IsActive)
        {
            var hasActive = await _dbContext.Contracts
                .AsNoTracking()
                .AnyAsync(x => x.CompanyProfileId == company.Id && x.IsActive);

            if (hasActive)
            {
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { nameof(request.IsActive), new[] { "Company already has an active contract." } }
                }));
            }
        }

        var serviceIds = request.Services.Select(x => x.ServiceId).Distinct().ToList();
        var existingServices = await _dbContext.Services
            .AsNoTracking()
            .Where(x => serviceIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();

        if (existingServices.Count != serviceIds.Count)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(request.Services), new[] { "One or more services are invalid." } }
            }));
        }

        var assetIds = request.AssetIds.Distinct().ToList();
        if (assetIds.Count > 0)
        {
            var existingAssets = await _dbContext.Assets
                .AsNoTracking()
                .Where(x => x.CompanyProfileId == company.Id && assetIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            if (existingAssets.Count != assetIds.Count)
            {
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { nameof(request.AssetIds), new[] { "One or more assets are invalid for this company." } }
                }));
            }
        }

        var contract = new Contract
        {
            CompanyProfileId = company.Id,
            MonthlyPrice = request.MonthlyPrice,
            IncludedSupportMinutesPerMonth = request.IncludedSupportMinutesPerMonth,
            OnsiteDaysIncluded = request.OnsiteDaysIncluded,
            IsActive = request.IsActive
        };

        _dbContext.Contracts.Add(contract);

        var contractServices = request.Services.Select(service => new ContractService
        {
            ContractId = contract.Id,
            ServiceCatalogItemId = service.ServiceId,
            CustomFirstResponseMinutes = service.CustomFirstResponseMinutes,
            CustomResolutionMinutes = service.CustomResolutionMinutes
        }).ToList();

        _dbContext.ContractServices.AddRange(contractServices);

        if (assetIds.Count > 0)
        {
            var contractAssets = assetIds.Select(assetId => new ContractAsset
            {
                ContractId = contract.Id,
                AssetId = assetId
            }).ToList();

            _dbContext.ContractAssets.AddRange(contractAssets);
        }

        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateContract([FromRoute] Guid id, [FromBody] UpdateContractRequest request)
    {
        var contract = await _dbContext.Contracts
            .FirstOrDefaultAsync(x => x.Id == id);

        if (contract is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Contract not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (request.IsActive && !contract.IsActive)
        {
            var hasActive = await _dbContext.Contracts
                .AsNoTracking()
                .AnyAsync(x => x.CompanyProfileId == contract.CompanyProfileId && x.IsActive && x.Id != contract.Id);

            if (hasActive)
            {
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { nameof(request.IsActive), new[] { "Company already has an active contract." } }
                }));
            }
        }

        var serviceIds = request.Services.Select(x => x.ServiceId).Distinct().ToList();
        var existingServices = await _dbContext.Services
            .AsNoTracking()
            .Where(x => serviceIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();

        if (existingServices.Count != serviceIds.Count)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(request.Services), new[] { "One or more services are invalid." } }
            }));
        }

        var assetIds = request.AssetIds.Distinct().ToList();
        if (assetIds.Count > 0)
        {
            var existingAssets = await _dbContext.Assets
                .AsNoTracking()
                .Where(x => x.CompanyProfileId == contract.CompanyProfileId && assetIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            if (existingAssets.Count != assetIds.Count)
            {
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { nameof(request.AssetIds), new[] { "One or more assets are invalid for this company." } }
                }));
            }
        }

        contract.MonthlyPrice = request.MonthlyPrice;
        contract.IncludedSupportMinutesPerMonth = request.IncludedSupportMinutesPerMonth;
        contract.OnsiteDaysIncluded = request.OnsiteDaysIncluded;
        contract.IsActive = request.IsActive;

        var existingContractServices = await _dbContext.ContractServices
            .Where(x => x.ContractId == contract.Id)
            .ToListAsync();
        _dbContext.ContractServices.RemoveRange(existingContractServices);

        var newContractServices = request.Services.Select(service => new ContractService
        {
            ContractId = contract.Id,
            ServiceCatalogItemId = service.ServiceId,
            CustomFirstResponseMinutes = service.CustomFirstResponseMinutes,
            CustomResolutionMinutes = service.CustomResolutionMinutes
        }).ToList();
        _dbContext.ContractServices.AddRange(newContractServices);

        var existingContractAssets = await _dbContext.ContractAssets
            .Where(x => x.ContractId == contract.Id)
            .ToListAsync();
        _dbContext.ContractAssets.RemoveRange(existingContractAssets);

        if (assetIds.Count > 0)
        {
            var newContractAssets = assetIds.Select(assetId => new ContractAsset
            {
                ContractId = contract.Id,
                AssetId = assetId
            }).ToList();
            _dbContext.ContractAssets.AddRange(newContractAssets);
        }

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateContract([FromRoute] Guid id)
    {
        var contract = await _dbContext.Contracts
            .FirstOrDefaultAsync(x => x.Id == id);

        if (contract is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Contract not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var hasActive = await _dbContext.Contracts
            .AsNoTracking()
            .AnyAsync(x => x.CompanyProfileId == contract.CompanyProfileId && x.IsActive && x.Id != contract.Id);

        if (hasActive)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { "IsActive", new[] { "Company already has an active contract." } }
            }));
        }

        contract.IsActive = true;
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private async Task<Dictionary<Guid, List<ContractServiceDto>>> LoadContractServicesAsync(Guid[] contractIds)
    {
        if (contractIds.Length == 0)
        {
            return new Dictionary<Guid, List<ContractServiceDto>>();
        }

        var services = new List<(Guid ContractId, ContractServiceDto Service)>();
        var connectionString = _dbContext.Database.GetConnectionString();
        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = BuildContractServiceQuery(contractIds);
            AddGuidParameters(command, "p", contractIds);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var contractId = reader.GetGuid(0);
                var service = new ContractServiceDto
                {
                    ServiceId = reader.GetGuid(1),
                    ServiceName = reader.GetString(2),
                    DefaultFirstResponseMinutes = reader.GetInt32(3),
                    DefaultResolutionMinutes = reader.GetInt32(4),
                    CustomFirstResponseMinutes = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    CustomResolutionMinutes = reader.IsDBNull(6) ? null : reader.GetInt32(6)
                };
                services.Add((contractId, service));
            }
        }

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

        var assets = new List<(Guid ContractId, ContractAssetDto Asset)>();
        var connectionString = _dbContext.Database.GetConnectionString();
        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = BuildContractAssetQuery(contractIds);
            AddGuidParameters(command, "p", contractIds);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var contractId = reader.GetGuid(0);
                var asset = new ContractAssetDto
                {
                    AssetId = reader.GetGuid(1),
                    AssetName = reader.GetString(2),
                    AssetTag = reader.IsDBNull(3) ? null : reader.GetString(3),
                    SerialNumber = reader.IsDBNull(4) ? null : reader.GetString(4)
                };
                assets.Add((contractId, asset));
            }
        }

        return assets
            .GroupBy(x => x.ContractId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Asset).ToList());
    }

    private async Task<Dictionary<Guid, string>> LoadCompanyNamesAsync(Guid[] companyIds)
    {
        if (companyIds.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var results = new Dictionary<Guid, string>();
        var connectionString = _dbContext.Database.GetConnectionString();
        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = BuildCompanyQuery(companyIds);
            AddGuidParameters(command, "p", companyIds);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results[reader.GetGuid(0)] = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            }
        }

        return results;
    }

    private static string BuildCompanyQuery(Guid[] ids)
    {
        return $"SELECT Id, CompanyName FROM CompanyProfiles WHERE Id IN ({BuildInClause(ids.Length)})";
    }

    private static string BuildContractServiceQuery(Guid[] ids)
    {
        return
            $"SELECT cs.ContractId, s.Id, s.Name, s.DefaultFirstResponseMinutes, s.DefaultResolutionMinutes, " +
            $"cs.CustomFirstResponseMinutes, cs.CustomResolutionMinutes " +
            $"FROM ContractServices cs " +
            $"INNER JOIN Services s ON cs.ServiceCatalogItemId = s.Id " +
            $"WHERE cs.ContractId IN ({BuildInClause(ids.Length)})";
    }

    private static string BuildContractAssetQuery(Guid[] ids)
    {
        return
            $"SELECT ca.ContractId, a.Id, a.Name, a.AssetTag, a.SerialNumber " +
            $"FROM ContractAssets ca " +
            $"INNER JOIN Assets a ON ca.AssetId = a.Id " +
            $"WHERE ca.ContractId IN ({BuildInClause(ids.Length)})";
    }

    private static string BuildInClause(int count)
    {
        return string.Join(", ", Enumerable.Range(0, count).Select(i => $"@p{i}"));
    }

    private static void AddGuidParameters(DbCommand command, string prefix, IReadOnlyList<Guid> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"@{prefix}{i}";
            parameter.DbType = DbType.Guid;
            parameter.Value = values[i];
            command.Parameters.Add(parameter);
        }
    }
}
