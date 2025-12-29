using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebApi.Configuration;
using WebApi.Contracts.Common;
using WebApi.Contracts.Tickets;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("company/tickets")]
[Authorize(Roles = $"{RoleNames.Company},{RoleNames.Admin}")]
public class CompanyTicketsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SlaOptions _slaOptions;
    private readonly ActingUserContext _actingUserContext;

    public CompanyTicketsController(
        ApplicationDbContext dbContext,
        IOptions<SlaOptions> slaOptions,
        ActingUserContext actingUserContext)
    {
        _dbContext = dbContext;
        _slaOptions = slaOptions.Value;
        _actingUserContext = actingUserContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketDto>>> GetTickets(
        [FromQuery] TicketStatus? status,
        [FromQuery] string? search,
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

        var query = _dbContext.Tickets
            .AsNoTracking()
            .Where(x => x.CompanyProfileId == company.Id);

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

        var items = await MapTicketsAsync(tickets, company.CompanyName);

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
        var (company, errorResult) = await ResolveCompanyAsync(asNoTracking: true);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyProfileId == company.Id && x.Id == id);

        if (ticket is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var items = await MapTicketsAsync(new List<Ticket> { ticket }, company.CompanyName);
        return Ok(items[0]);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var (company, errorResult) = await ResolveCompanyAsync(asNoTracking: false);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var userId = _actingUserContext.GetEffectiveCompanyUserId() ?? string.Empty;

        Asset? asset = null;
        if (request.AssetId.HasValue)
        {
            asset = await _dbContext.Assets
                .FirstOrDefaultAsync(x => x.CompanyProfileId == company.Id && x.Id == request.AssetId.Value);

            if (asset is null)
            {
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { nameof(request.AssetId), new[] { "Asset is invalid for this company." } }
                }));
            }
        }

        ServiceCatalogItem? service = null;
        if (request.ServiceId.HasValue)
        {
            service = await _dbContext.Services
                .FirstOrDefaultAsync(x => x.Id == request.ServiceId.Value && x.IsActive);

            if (service is null)
            {
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { nameof(request.ServiceId), new[] { "Service is invalid or inactive." } }
                }));
            }
        }

        var slaFirstResponse = _slaOptions.FirstResponseMinutes;
        var slaResolution = _slaOptions.ResolutionMinutes;

        if (service is not null)
        {
            slaFirstResponse = service.DefaultFirstResponseMinutes;
            slaResolution = service.DefaultResolutionMinutes;

            var activeContract = await _dbContext.Contracts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyProfileId == company.Id && x.IsActive);

            if (activeContract is not null)
            {
                var contractService = await _dbContext.ContractServices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ContractId == activeContract.Id && x.ServiceCatalogItemId == service.Id);

                if (contractService is not null)
                {
                    if (contractService.CustomFirstResponseMinutes.HasValue)
                    {
                        slaFirstResponse = contractService.CustomFirstResponseMinutes.Value;
                    }

                    if (contractService.CustomResolutionMinutes.HasValue)
                    {
                        slaResolution = contractService.CustomResolutionMinutes.Value;
                    }
                }
            }
        }

        var now = DateTime.UtcNow;
        var assignedExpertProfileId = await ResolveAssignmentAsync(company.Id, asset);

        var ticket = new Ticket
        {
            CompanyProfileId = company.Id,
            ServiceCatalogItemId = service?.Id,
            AssetId = asset?.Id,
            AssignedExpertProfileId = assignedExpertProfileId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = TicketStatus.Open,
            SlaFirstResponseMinutes = slaFirstResponse,
            SlaResolutionMinutes = slaResolution,
            FirstResponseDueAtUtc = now.AddMinutes(slaFirstResponse),
            ResolutionDueAtUtc = now.AddMinutes(slaResolution)
        };

        _dbContext.Tickets.Add(ticket);

        _dbContext.TicketMessages.Add(new TicketMessage
        {
            TicketId = ticket.Id,
            AuthorUserId = userId,
            AuthorRole = TicketMessageAuthorRole.Company,
            Body = ticket.Description,
            IsInternal = false
        });

        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<IReadOnlyList<TicketMessageDto>>> GetMessages([FromRoute] Guid id)
    {
        var (company, errorResult) = await ResolveCompanyAsync(asNoTracking: true);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var exists = await _dbContext.Tickets
            .AsNoTracking()
            .AnyAsync(x => x.CompanyProfileId == company.Id && x.Id == id);

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
            .Where(x => x.TicketId == id && !x.IsInternal)
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
        var (company, errorResult) = await ResolveCompanyAsync(asNoTracking: false);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var userId = _actingUserContext.GetEffectiveCompanyUserId() ?? string.Empty;

        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(x => x.CompanyProfileId == company.Id && x.Id == id);

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

        _dbContext.TicketMessages.Add(new TicketMessage
        {
            TicketId = ticket.Id,
            AuthorUserId = userId,
            AuthorRole = TicketMessageAuthorRole.Company,
            Body = request.Body.Trim(),
            IsInternal = false
        });

        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus([FromRoute] Guid id, [FromBody] UpdateTicketStatusRequest request)
    {
        var (company, errorResult) = await ResolveCompanyAsync(asNoTracking: false);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(x => x.CompanyProfileId == company.Id && x.Id == id);

        if (ticket is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (request.Status != TicketStatus.Closed || ticket.Status != TicketStatus.Resolved)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(request.Status), new[] { "Company can only close resolved tickets." } }
            }));
        }

        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:guid}/satisfaction")]
    public async Task<IActionResult> SubmitSatisfaction(
        [FromRoute] Guid id,
        [FromBody] CreateTicketSatisfactionRequest request)
    {
        var (company, errorResult) = await ResolveCompanyAsync(asNoTracking: false);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(x => x.CompanyProfileId == company.Id && x.Id == id);

        if (ticket is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (ticket.Status != TicketStatus.Closed)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(ticket.Status), new[] { "Ticket must be closed before submitting satisfaction." } }
            }));
        }

        var exists = await _dbContext.TicketSatisfactions
            .AsNoTracking()
            .AnyAsync(x => x.TicketId == ticket.Id);

        if (exists)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(id), new[] { "Satisfaction has already been submitted." } }
            }));
        }

        _dbContext.TicketSatisfactions.Add(new TicketSatisfaction
        {
            TicketId = ticket.Id,
            CompanyProfileId = company.Id,
            Rating = request.Rating,
            ResponseTimeRating = request.ResponseTimeRating,
            ResolutionQualityRating = request.ResolutionQualityRating,
            CommunicationRating = request.CommunicationRating,
            Comment = request.Comment?.Trim()
        });

        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet("satisfaction")]
    public async Task<ActionResult<PagedResult<ClosedTicketSatisfactionDto>>> GetSatisfactionTickets(
        [FromQuery] string? search,
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

        var query = _dbContext.Tickets
            .AsNoTracking()
            .Where(x => x.CompanyProfileId == company.Id && x.Status == TicketStatus.Closed);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.Like(x.Title, $"%{term}%"));
        }

        var totalCount = await query.CountAsync();
        var tickets = await query
            .OrderByDescending(x => x.ClosedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var ticketIds = tickets.Select(x => x.Id).ToArray();
        var satisfactions = await _dbContext.TicketSatisfactions
            .AsNoTracking()
            .Where(x => ticketIds.Contains(x.TicketId))
            .ToListAsync();

        var satisfactionLookup = satisfactions.ToDictionary(x => x.TicketId, x => x);

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

        var services = await _dbContext.Services
            .AsNoTracking()
            .Where(x => serviceIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var assets = await _dbContext.Assets
            .AsNoTracking()
            .Where(x => assetIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var items = tickets.Select(ticket =>
        {
            satisfactionLookup.TryGetValue(ticket.Id, out var satisfaction);
            return new ClosedTicketSatisfactionDto
            {
                TicketId = ticket.Id,
                Title = ticket.Title,
                ServiceName = ticket.ServiceCatalogItemId.HasValue &&
                              services.TryGetValue(ticket.ServiceCatalogItemId.Value, out var serviceName)
                    ? serviceName
                    : null,
                AssetName = ticket.AssetId.HasValue &&
                            assets.TryGetValue(ticket.AssetId.Value, out var assetName)
                    ? assetName
                    : null,
                ClosedAtUtc = ticket.ClosedAtUtc ?? DateTime.UtcNow,
                SatisfactionSubmitted = satisfaction is not null,
                Rating = satisfaction?.Rating,
                ResponseTimeRating = satisfaction?.ResponseTimeRating,
                ResolutionQualityRating = satisfaction?.ResolutionQualityRating,
                CommunicationRating = satisfaction?.CommunicationRating,
                Comment = satisfaction?.Comment
            };
        }).ToList();

        return Ok(new PagedResult<ClosedTicketSatisfactionDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
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

    private async Task<Guid?> ResolveAssignmentAsync(Guid companyProfileId, Asset? asset)
    {
        if (asset?.PrimaryExpertProfileId is not null)
        {
            var assetExpertId = asset.PrimaryExpertProfileId.Value;
            var assetExpertApproved = await _dbContext.ExpertProfiles
                .AsNoTracking()
                .AnyAsync(x => x.Id == assetExpertId && x.IsApproved);

            if (assetExpertApproved)
            {
                var linked = await _dbContext.CompanyExpertLinks
                    .AsNoTracking()
                    .AnyAsync(x => x.CompanyProfileId == companyProfileId && x.ExpertProfileId == assetExpertId);

                if (linked)
                {
                    return assetExpertId;
                }
            }
        }

        var companyPrimaryExpertId = await _dbContext.CompanyExpertLinks
            .AsNoTracking()
            .Where(x => x.CompanyProfileId == companyProfileId && x.IsPrimary)
            .Select(x => x.ExpertProfileId)
            .FirstOrDefaultAsync();

        if (companyPrimaryExpertId == Guid.Empty)
        {
            return null;
        }

        var approved = await _dbContext.ExpertProfiles
            .AsNoTracking()
            .AnyAsync(x => x.Id == companyPrimaryExpertId && x.IsApproved);

        return approved ? companyPrimaryExpertId : null;
    }

    private async Task<List<TicketDto>> MapTicketsAsync(IReadOnlyList<Ticket> tickets, string companyName)
    {
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
            CompanyName = companyName,
            ServiceId = ticket.ServiceCatalogItemId,
            ServiceName = ticket.ServiceCatalogItemId.HasValue && services.TryGetValue(ticket.ServiceCatalogItemId.Value, out var serviceName)
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
}
