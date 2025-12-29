using Domain.Constants;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Experts;
using WebApi.Contracts.Skills;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("expert/skills")]
[Authorize(Roles = $"{RoleNames.Expert},{RoleNames.Admin}")]
public class ExpertSkillsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ActingUserContext _actingUserContext;

    public ExpertSkillsController(ApplicationDbContext dbContext, ActingUserContext actingUserContext)
    {
        _dbContext = dbContext;
        _actingUserContext = actingUserContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SkillDto>>> GetMySkills()
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

        var profile = await _dbContext.ExpertProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Expert profile not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var skills = await _dbContext.ExpertSkills
            .AsNoTracking()
            .Where(x => x.ExpertProfileId == profile.Id)
            .Join(_dbContext.Skills.AsNoTracking(),
                expertSkill => expertSkill.SkillId,
                skill => skill.Id,
                (_, skill) => new SkillDto
                {
                    Id = skill.Id,
                    Name = skill.Name,
                    Description = skill.Description
                })
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(skills);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateMySkills([FromBody] ExpertSkillUpdateRequest request)
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

        var profile = await _dbContext.ExpertProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Expert profile not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (!profile.IsApproved)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Expert approval required.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        var skillIds = request.SkillIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (skillIds.Count == 0)
        {
            var existing = await _dbContext.ExpertSkills
                .Where(x => x.ExpertProfileId == profile.Id)
                .ToListAsync();

            _dbContext.ExpertSkills.RemoveRange(existing);
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }

        var skills = await _dbContext.Skills
            .AsNoTracking()
            .Where(x => skillIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();

        if (skills.Count != skillIds.Count)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(request.SkillIds), new[] { "One or more skills are invalid." } }
            }));
        }

        var existingLinks = await _dbContext.ExpertSkills
            .Where(x => x.ExpertProfileId == profile.Id)
            .ToListAsync();

        _dbContext.ExpertSkills.RemoveRange(existingLinks);

        var newLinks = skillIds.Select(skillId => new ExpertSkill
        {
            ExpertProfileId = profile.Id,
            SkillId = skillId
        });

        _dbContext.ExpertSkills.AddRange(newLinks);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
