using Domain.Constants;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Kpi;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("expert/kpi")]
[Authorize(Roles = $"{RoleNames.Expert},{RoleNames.Admin}")]
public class ExpertKpiController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ActingUserContext _actingUserContext;

    public ExpertKpiController(ApplicationDbContext dbContext, ActingUserContext actingUserContext)
    {
        _dbContext = dbContext;
        _actingUserContext = actingUserContext;
    }

    [HttpGet]
    public async Task<ActionResult<ExpertKpiDto>> GetKpi()
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

        var ticketQuery = _dbContext.Tickets
            .AsNoTracking()
            .Where(x => x.AssignedExpertProfileId == expert.Id);

        var totalAssigned = await ticketQuery.CountAsync();
        var totalResolved = await ticketQuery.CountAsync(x => x.Status == Domain.Enums.TicketStatus.Resolved ||
                                                              x.Status == Domain.Enums.TicketStatus.Closed);
        var firstResponseAvg = await ticketQuery
            .Where(x => x.FirstResponseAtUtc.HasValue)
            .AverageAsync(x => (double?)EF.Functions.DateDiffMinute(x.CreatedAtUtc, x.FirstResponseAtUtc!.Value));
        var resolutionAvg = await ticketQuery
            .Where(x => x.ResolvedAtUtc.HasValue)
            .AverageAsync(x => (double?)EF.Functions.DateDiffMinute(x.CreatedAtUtc, x.ResolvedAtUtc!.Value));
        var firstResponseBreaches = await ticketQuery.CountAsync(x => x.FirstResponseBreached);
        var resolutionBreaches = await ticketQuery.CountAsync(x => x.ResolutionBreached);

        var satisfactionQuery = from satisfaction in _dbContext.TicketSatisfactions.AsNoTracking()
                                join ticket in _dbContext.Tickets.AsNoTracking()
                                    on satisfaction.TicketId equals ticket.Id
                                where ticket.AssignedExpertProfileId == expert.Id
                                select satisfaction;

        var satisfactionCount = await satisfactionQuery.CountAsync();
        var averageRating = await satisfactionQuery.AverageAsync(x => (double?)x.Rating);
        var averageResponseRating = await satisfactionQuery
            .Where(x => x.ResponseTimeRating.HasValue)
            .AverageAsync(x => (double?)x.ResponseTimeRating);
        var averageResolutionRating = await satisfactionQuery
            .Where(x => x.ResolutionQualityRating.HasValue)
            .AverageAsync(x => (double?)x.ResolutionQualityRating);
        var averageCommunicationRating = await satisfactionQuery
            .Where(x => x.CommunicationRating.HasValue)
            .AverageAsync(x => (double?)x.CommunicationRating);

        var totalLoggedMinutes = await _dbContext.TicketTimeLogs
            .AsNoTracking()
            .Where(x => x.ExpertProfileId == expert.Id)
            .SumAsync(x => (int?)x.Minutes) ?? 0;

        return Ok(new ExpertKpiDto
        {
            ExpertProfileId = expert.Id,
            TotalAssignedTickets = totalAssigned,
            TotalResolvedTickets = totalResolved,
            AverageFirstResponseMinutes = firstResponseAvg,
            AverageResolutionMinutes = resolutionAvg,
            FirstResponseBreachCount = firstResponseBreaches,
            ResolutionBreachCount = resolutionBreaches,
            SatisfactionCount = satisfactionCount,
            AverageRating = averageRating,
            AverageResponseTimeRating = averageResponseRating,
            AverageResolutionQualityRating = averageResolutionRating,
            AverageCommunicationRating = averageCommunicationRating,
            TotalLoggedMinutes = totalLoggedMinutes
        });
    }
}
