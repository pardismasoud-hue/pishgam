using System.Security.Claims;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using WebApi.Contracts.Common;
using WebApi.Contracts.Tickets;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("admin/tickets")]
[Authorize(Roles = RoleNames.Admin)]
public class AdminTicketsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AdminTicketsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketDto>>> GetTickets(
        [FromQuery] TicketStatus? status,
        [FromQuery] string? companyId,
        [FromQuery] string? expertId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Tickets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(companyId))
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

            query = query.Where(x => x.CompanyProfileId == company.Id);
        }

        if (!string.IsNullOrWhiteSpace(expertId))
        {
            var expert = await _dbContext.ExpertProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == expertId);

            if (expert is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Expert profile not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            query = query.Where(x => x.AssignedExpertProfileId == expert.Id);
        }

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
        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

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
        var exists = await _dbContext.Tickets
            .AsNoTracking()
            .AnyAsync(x => x.Id == id);

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
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(x => x.Id == id);

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

        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? string.Empty;
        _dbContext.TicketMessages.Add(new TicketMessage
        {
            TicketId = ticket.Id,
            AuthorUserId = userId,
            AuthorRole = TicketMessageAuthorRole.Admin,
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

    [HttpGet("{id:guid}/timelogs")]
    public async Task<ActionResult<IReadOnlyList<TicketTimeLogDto>>> GetTimeLogs([FromRoute] Guid id)
    {
        var exists = await _dbContext.Tickets
            .AsNoTracking()
            .AnyAsync(x => x.Id == id);

        if (!exists)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var logs = await _dbContext.TicketTimeLogs
            .AsNoTracking()
            .Where(x => x.TicketId == id)
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
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(x => x.Id == id);

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

    [HttpPost("{id:guid}/assign/{expertId}")]
    public async Task<IActionResult> AssignTicket([FromRoute] Guid id, [FromRoute] string expertId)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(x => x.Id == id);

        if (ticket is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var expertProfile = await _dbContext.ExpertProfiles
            .FirstOrDefaultAsync(x => x.UserId == expertId);

        if (expertProfile is null || !expertProfile.IsApproved)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(expertId), new[] { "Expert is not approved or not found." } }
            }));
        }

        var linked = await _dbContext.CompanyExpertLinks
            .AsNoTracking()
            .AnyAsync(x => x.CompanyProfileId == ticket.CompanyProfileId && x.ExpertProfileId == expertProfile.Id);

        if (!linked)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(expertId), new[] { "Expert is not linked to this company." } }
            }));
        }

        ticket.AssignedExpertProfileId = expertProfile.Id;
        await _dbContext.SaveChangesAsync();

        return NoContent();
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

        var companies = await LoadCompanyNamesAsync(companyIds);
        var services = await LoadServiceNamesAsync(serviceIds);
        var assets = await LoadAssetNamesAsync(assetIds);
        var experts = await LoadExpertNamesAsync(expertIds);

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

    private async Task<Dictionary<Guid, string>> LoadCompanyNamesAsync(Guid[] ids)
    {
        return await LoadNameLookupAsync("CompanyProfiles", "CompanyName", ids);
    }

    private async Task<Dictionary<Guid, string>> LoadServiceNamesAsync(Guid[] ids)
    {
        return await LoadNameLookupAsync("Services", "Name", ids);
    }

    private async Task<Dictionary<Guid, string>> LoadAssetNamesAsync(Guid[] ids)
    {
        return await LoadNameLookupAsync("Assets", "Name", ids);
    }

    private async Task<Dictionary<Guid, string>> LoadExpertNamesAsync(Guid[] ids)
    {
        return await LoadNameLookupAsync("ExpertProfiles", "FullName", ids);
    }

    private async Task<Dictionary<Guid, string>> LoadNameLookupAsync(string table, string nameColumn, Guid[] ids)
    {
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var results = new Dictionary<Guid, string>();
        var connectionString = _dbContext.Database.GetConnectionString();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT Id, {nameColumn} FROM {table} WHERE Id IN ({BuildInClause(ids.Length)})";
        AddGuidParameters(command, "p", ids);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results[reader.GetGuid(0)] = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        }

        return results;
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
