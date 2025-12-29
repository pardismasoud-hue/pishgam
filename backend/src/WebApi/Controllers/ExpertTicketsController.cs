using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Common;
using WebApi.Contracts.Tickets;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("expert/tickets")]
[Authorize(Roles = $"{RoleNames.Expert},{RoleNames.Admin}")]
public class ExpertTicketsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ActingUserContext _actingUserContext;

    public ExpertTicketsController(ApplicationDbContext dbContext, ActingUserContext actingUserContext)
    {
        _dbContext = dbContext;
        _actingUserContext = actingUserContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketDto>>> GetTickets(
        [FromQuery] TicketStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (expert, expertError) = await ResolveExpertAsync();
        if (expertError is not null)
        {
            return expertError;
        }

        if (!expert.IsApproved)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Expert approval required.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Tickets
            .AsNoTracking()
            .Where(x => x.AssignedExpertProfileId == expert.Id);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.Like(x.Title, $"%{term}%"));
        }

        var totalCount = await query.CountAsync();
        var tickets = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = await MapTicketsAsync(tickets);

        return Ok(new PagedResult<TicketDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketDto>> GetTicket([FromRoute] Guid id)
    {
        var (expert, expertError) = await ResolveExpertAsync();
        if (expertError is not null)
        {
            return expertError;
        }

        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AssignedExpertProfileId == expert.Id && x.Id == id);

        if (ticket is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var items = await MapTicketsAsync(new List<Ticket> { ticket });
        return Ok(items[0]);
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<IReadOnlyList<TicketMessageDto>>> GetMessages([FromRoute] Guid id)
    {
        var (expert, expertError) = await ResolveExpertAsync();
        if (expertError is not null)
        {
            return expertError;
        }

        var exists = await _dbContext.Tickets
            .AsNoTracking()
            .AnyAsync(x => x.AssignedExpertProfileId == expert.Id && x.Id == id);

        if (!exists)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var messages = await _dbContext.TicketMessages
            .AsNoTracking()
            .Where(x => x.TicketId == id)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new TicketMessageDto
            {
                Id = x.Id,
                TicketId = x.TicketId,
                AuthorUserId = x.AuthorUserId,
                AuthorRole = x.AuthorRole,
                Body = x.Body,
                IsInternal = x.IsInternal,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(messages);
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> AddMessage([FromRoute] Guid id, [FromBody] CreateTicketMessageRequest request)
    {
        var (expert, expertError) = await ResolveExpertAsync();
        if (expertError is not null)
        {
            return expertError;
        }

        if (!expert.IsApproved)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Expert approval required.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(x => x.AssignedExpertProfileId == expert.Id && x.Id == id);

        if (ticket is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (ticket.Status == TicketStatus.Closed)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(ticket.Status), new[] { "Ticket is closed." } }
            }));
        }

        var userId = _actingUserContext.GetEffectiveExpertUserId() ?? string.Empty;
        _dbContext.TicketMessages.Add(new TicketMessage
        {
            TicketId = ticket.Id,
            AuthorUserId = userId,
            AuthorRole = TicketMessageAuthorRole.Expert,
            Body = request.Body.Trim(),
            IsInternal = request.IsInternal
        });

        if (ticket.FirstResponseAtUtc is null)
        {
            var now = DateTime.UtcNow;
            ticket.FirstResponseAtUtc = now;
            ticket.FirstResponseBreached = now > ticket.FirstResponseDueAtUtc;
        }

        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("{id:guid}/timelogs")]
    public async Task<IActionResult> AddTimeLog([FromRoute] Guid id, [FromBody] CreateTicketTimeLogRequest request)
    {
        var (expert, expertError) = await ResolveExpertAsync();
        if (expertError is not null)
        {
            return expertError;
        }

        if (!expert.IsApproved)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Expert approval required.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        var ticketExists = await _dbContext.Tickets
            .AsNoTracking()
            .AnyAsync(x => x.AssignedExpertProfileId == expert.Id && x.Id == id);

        if (!ticketExists)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        _dbContext.TicketTimeLogs.Add(new TicketTimeLog
        {
            TicketId = id,
            ExpertProfileId = expert.Id,
            Minutes = request.Minutes,
            WorkType = request.WorkType,
            LoggedAtUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet("{id:guid}/timelogs")]
    public async Task<ActionResult<IReadOnlyList<TicketTimeLogDto>>> GetTimeLogs([FromRoute] Guid id)
    {
        var (expert, expertError) = await ResolveExpertAsync();
        if (expertError is not null)
        {
            return expertError;
        }

        var ticketExists = await _dbContext.Tickets
            .AsNoTracking()
            .AnyAsync(x => x.AssignedExpertProfileId == expert.Id && x.Id == id);

        if (!ticketExists)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var logs = await _dbContext.TicketTimeLogs
            .AsNoTracking()
            .Where(x => x.TicketId == id && x.ExpertProfileId == expert.Id)
            .OrderByDescending(x => x.LoggedAtUtc)
            .Select(x => new TicketTimeLogDto
            {
                Id = x.Id,
                TicketId = x.TicketId,
                ExpertProfileId = x.ExpertProfileId,
                Minutes = x.Minutes,
                WorkType = x.WorkType,
                LoggedAtUtc = x.LoggedAtUtc
            })
            .ToListAsync();

        return Ok(logs);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus([FromRoute] Guid id, [FromBody] UpdateTicketStatusRequest request)
    {
        var (expert, expertError) = await ResolveExpertAsync();
        if (expertError is not null)
        {
            return expertError;
        }

        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(x => x.AssignedExpertProfileId == expert.Id && x.Id == id);

        if (ticket is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (!TicketWorkflow.IsValidTransition(ticket.Status, request.Status))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(request.Status), new[] { "Invalid status transition." } }
            }));
        }

        ApplyStatusChange(ticket, request.Status);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private async Task<(ExpertProfile? Expert, ActionResult? ErrorResult)> ResolveExpertAsync()
    {
        var userId = _actingUserContext.GetEffectiveExpertUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return (null, BadRequest(new ProblemDetails
            {
                Title = "Expert context is required.",
                Detail = "Select an expert to act as.",
                Status = StatusCodes.Status400BadRequest
            }));
        }

        var expert = await _dbContext.ExpertProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (expert is null)
        {
            return (null, NotFound(new ProblemDetails
            {
                Title = "Expert profile not found.",
                Status = StatusCodes.Status404NotFound
            }));
        }

        return (expert, null);
    }

    private async Task<List<TicketDto>> MapTicketsAsync(IReadOnlyList<Ticket> tickets)
    {
        var companyIds = tickets.Select(x => x.CompanyProfileId).Distinct().ToArray();
        var serviceIds = tickets
            .Where(x => x.ServiceCatalogItemId.HasValue)
            .Select(x => x.ServiceCatalogItemId!.Value)
            .Distinct()
            .ToArray();
        var assetIds = tickets
            .Where(x => x.AssetId.HasValue)
            .Select(x => x.AssetId!.Value)
            .Distinct()
            .ToArray();
        var expertIds = tickets
            .Where(x => x.AssignedExpertProfileId.HasValue)
            .Select(x => x.AssignedExpertProfileId!.Value)
            .Distinct()
            .ToArray();

        var companies = await _dbContext.CompanyProfiles
            .AsNoTracking()
            .Where(x => companyIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.CompanyName);

        var services = await _dbContext.Services
            .AsNoTracking()
            .Where(x => serviceIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var assets = await _dbContext.Assets
            .AsNoTracking()
            .Where(x => assetIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var experts = await _dbContext.ExpertProfiles
            .AsNoTracking()
            .Where(x => expertIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName);

        return tickets.Select(ticket => new TicketDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            CompanyProfileId = ticket.CompanyProfileId,
            CompanyName = companies.TryGetValue(ticket.CompanyProfileId, out var companyName)
                ? companyName
                : string.Empty,
            ServiceId = ticket.ServiceCatalogItemId,
            ServiceName = ticket.ServiceCatalogItemId.HasValue &&
                          services.TryGetValue(ticket.ServiceCatalogItemId.Value, out var serviceName)
                ? serviceName
                : null,
            AssetId = ticket.AssetId,
            AssetName = ticket.AssetId.HasValue && assets.TryGetValue(ticket.AssetId.Value, out var assetName)
                ? assetName
                : null,
            AssignedExpertProfileId = ticket.AssignedExpertProfileId,
            AssignedExpertName = ticket.AssignedExpertProfileId.HasValue &&
                                 experts.TryGetValue(ticket.AssignedExpertProfileId.Value, out var expertName)
                ? expertName
                : null,
            SlaFirstResponseMinutes = ticket.SlaFirstResponseMinutes,
            SlaResolutionMinutes = ticket.SlaResolutionMinutes,
            FirstResponseDueAtUtc = ticket.FirstResponseDueAtUtc,
            ResolutionDueAtUtc = ticket.ResolutionDueAtUtc,
            FirstResponseAtUtc = ticket.FirstResponseAtUtc,
            ResolvedAtUtc = ticket.ResolvedAtUtc,
            ClosedAtUtc = ticket.ClosedAtUtc,
            FirstResponseBreached = ticket.FirstResponseBreached,
            ResolutionBreached = ticket.ResolutionBreached,
            CreatedAtUtc = ticket.CreatedAtUtc
        }).ToList();
    }

    private static void ApplyStatusChange(Ticket ticket, TicketStatus status)
    {
        ticket.Status = status;
        var now = DateTime.UtcNow;

        if (status == TicketStatus.Resolved)
        {
            ticket.ResolvedAtUtc = now;
            ticket.ResolutionBreached = now > ticket.ResolutionDueAtUtc;
        }
        else if (status == TicketStatus.Closed)
        {
            ticket.ClosedAtUtc = now;
        }
    }
}
