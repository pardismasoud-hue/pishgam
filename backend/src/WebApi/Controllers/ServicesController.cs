using Domain.Constants;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Common;
using WebApi.Contracts.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("services")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Expert},{RoleNames.Company}")]
public class ServicesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public ServicesController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ServiceDto>>> GetServices(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Services
            .AsNoTracking()
            .Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.Like(x.Name, $"%{term}%"));
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceDto>> GetService([FromRoute] Guid id)
    {
        var service = await _dbContext.Services
            .AsNoTracking()
            .Where(x => x.IsActive && x.Id == id)
            .Select(x => new ServiceDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                DefaultFirstResponseMinutes = x.DefaultFirstResponseMinutes,
                DefaultResolutionMinutes = x.DefaultResolutionMinutes,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();

        if (service is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Service not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(service);
    }
}
