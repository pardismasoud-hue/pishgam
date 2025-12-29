using Domain.Constants;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Common;
using WebApi.Contracts.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("admin/services")]
[Authorize(Roles = RoleNames.Admin)]
public class AdminServicesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AdminServicesController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ServiceDto>>> GetServices(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Services.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.Like(x.Name, $"%{term}%"));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ServiceDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                DefaultFirstResponseMinutes = x.DefaultFirstResponseMinutes,
                DefaultResolutionMinutes = x.DefaultResolutionMinutes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Ok(new PagedResult<ServiceDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request)
    {
        var name = request.Name.Trim();
        var exists = await _dbContext.Services.AnyAsync(x => x.Name == name);

        if (exists)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(request.Name), new[] { "Service name already exists." } }
            }));
        }

        var service = new ServiceCatalogItem
        {
            Name = name,
            Description = request.Description?.Trim(),
            DefaultFirstResponseMinutes = request.DefaultFirstResponseMinutes,
            DefaultResolutionMinutes = request.DefaultResolutionMinutes,
            IsActive = request.IsActive
        };

        _dbContext.Services.Add(service);
        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceDto>> UpdateService(
        [FromRoute] Guid id,
        [FromBody] UpdateServiceRequest request)
    {
        var service = await _dbContext.Services.FirstOrDefaultAsync(x => x.Id == id);
        if (service is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Service not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var name = request.Name.Trim();
        if (!string.Equals(service.Name, name, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _dbContext.Services.AnyAsync(x => x.Name == name);
            if (exists)
            {
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { nameof(request.Name), new[] { "Service name already exists." } }
                }));
            }
        }

        service.Name = name;
        service.Description = request.Description?.Trim();
        service.DefaultFirstResponseMinutes = request.DefaultFirstResponseMinutes;
        service.DefaultResolutionMinutes = request.DefaultResolutionMinutes;
        service.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync();

        return Ok(new ServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description,
            DefaultFirstResponseMinutes = service.DefaultFirstResponseMinutes,
            DefaultResolutionMinutes = service.DefaultResolutionMinutes,
            IsActive = service.IsActive
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteService([FromRoute] Guid id)
    {
        var service = await _dbContext.Services.FirstOrDefaultAsync(x => x.Id == id);
        if (service is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Service not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        _dbContext.Services.Remove(service);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
