using Domain.Constants;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Admin;

namespace WebApi.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Roles = RoleNames.Admin)]
public class AdminCompanyExpertsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AdminCompanyExpertsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("companies/{companyId}/experts/{expertId}/link")]
    public async Task<IActionResult> LinkExpertToCompany([FromRoute] string companyId, [FromRoute] string expertId)
    {
        var companyProfile = await _dbContext.CompanyProfiles
            .FirstOrDefaultAsync(x => x.UserId == companyId);

        if (companyProfile is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Company not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var expertProfile = await _dbContext.ExpertProfiles
            .FirstOrDefaultAsync(x => x.UserId == expertId);

        if (expertProfile is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Expert not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (!expertProfile.IsApproved)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(expertId), new[] { "Expert is not approved." } }
            }));
        }

        var exists = await _dbContext.CompanyExpertLinks
            .AnyAsync(x => x.CompanyProfileId == companyProfile.Id && x.ExpertProfileId == expertProfile.Id);

        if (exists)
        {
            return NoContent();
        }

        _dbContext.CompanyExpertLinks.Add(new CompanyExpertLink
        {
            CompanyProfileId = companyProfile.Id,
            ExpertProfileId = expertProfile.Id
        });

        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpDelete("company-expert-links/{linkId:guid}")]
    public async Task<IActionResult> DeleteCompanyExpertLink([FromRoute] Guid linkId)
    {
        var link = await _dbContext.CompanyExpertLinks.FirstOrDefaultAsync(x => x.Id == linkId);
        if (link is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Link not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        _dbContext.CompanyExpertLinks.Remove(link);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("companies/{companyId}/experts")]
    public async Task<ActionResult<IReadOnlyList<CompanyExpertDto>>> GetCompanyExperts([FromRoute] string companyId)
    {
        var companyProfile = await _dbContext.CompanyProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == companyId);

        if (companyProfile is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Company not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var experts = await _dbContext.CompanyExpertLinks
            .AsNoTracking()
            .Where(x => x.CompanyProfileId == companyProfile.Id)
            .Join(_dbContext.ExpertProfiles.AsNoTracking(),
                link => link.ExpertProfileId,
                expert => expert.Id,
                (link, expert) => new { link, expert })
            .Join(_dbContext.Users.AsNoTracking(),
                combined => combined.expert.UserId,
                user => user.Id,
                (combined, user) => new CompanyExpertDto
                {
                    LinkId = combined.link.Id,
                    ExpertUserId = user.Id,
                    ExpertProfileId = combined.expert.Id,
                    FullName = combined.expert.FullName,
                    Email = user.Email ?? string.Empty,
                    IsApproved = combined.expert.IsApproved,
                    IsPrimary = combined.link.IsPrimary
                })
            .OrderBy(x => x.FullName)
            .ToListAsync();

        return Ok(experts);
    }

    [HttpPost("company-expert-links/{linkId:guid}/primary")]
    public async Task<IActionResult> SetCompanyPrimaryExpert([FromRoute] Guid linkId)
    {
        var link = await _dbContext.CompanyExpertLinks
            .FirstOrDefaultAsync(x => x.Id == linkId);

        if (link is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Link not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var expert = await _dbContext.ExpertProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == link.ExpertProfileId);

        if (expert is null || !expert.IsApproved)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { "ExpertProfileId", new[] { "Expert is not approved." } }
            }));
        }

        var companyLinks = await _dbContext.CompanyExpertLinks
            .Where(x => x.CompanyProfileId == link.CompanyProfileId)
            .ToListAsync();

        foreach (var companyLink in companyLinks)
        {
            companyLink.IsPrimary = companyLink.Id == link.Id;
        }

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
