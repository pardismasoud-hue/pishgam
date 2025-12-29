using Domain.Constants;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Common;
using WebApi.Contracts.Skills;

namespace WebApi.Controllers;

[ApiController]
[Route("admin/skills")]
[Authorize(Roles = RoleNames.Admin)]
public class AdminSkillsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AdminSkillsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SkillDto>>> GetSkills(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Skills.AsNoTracking();

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
            .Select(x => new SkillDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description
            })
            .ToListAsync();

        return Ok(new PagedResult<SkillDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateSkill([FromBody] CreateSkillRequest request)
    {
        var exists = await _dbContext.Skills
            .AnyAsync(x => x.Name == request.Name);

        if (exists)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(request.Name), new[] { "Skill name already exists." } }
            }));
        }

        var skill = new Skill
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim()
        };

        _dbContext.Skills.Add(skill);
        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateSkill([FromRoute] Guid id, [FromBody] UpdateSkillRequest request)
    {
        var skill = await _dbContext.Skills.FirstOrDefaultAsync(x => x.Id == id);
        if (skill is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Skill not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var name = request.Name.Trim();
        if (!string.Equals(skill.Name, name, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _dbContext.Skills.AnyAsync(x => x.Name == name);
            if (exists)
            {
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { nameof(request.Name), new[] { "Skill name already exists." } }
                }));
            }
        }

        skill.Name = name;
        skill.Description = request.Description?.Trim();

        await _dbContext.SaveChangesAsync();

        return Ok(new SkillDto
        {
            Id = skill.Id,
            Name = skill.Name,
            Description = skill.Description
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSkill([FromRoute] Guid id)
    {
        var skill = await _dbContext.Skills.FirstOrDefaultAsync(x => x.Id == id);
        if (skill is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Skill not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        _dbContext.Skills.Remove(skill);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
