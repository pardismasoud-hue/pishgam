using Domain.Constants;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Common;
using WebApi.Contracts.Reports;

namespace WebApi.Controllers;

[ApiController]
[Route("admin/reports")]
[Authorize(Roles = RoleNames.Admin)]
public class AdminReportsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AdminReportsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("tickets-by-company")]
    public async Task<ActionResult<PagedResult<TicketsByCompanyReportRowDto>>> GetTicketsByCompany(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = from ticket in _dbContext.Tickets.AsNoTracking()
                    join company in _dbContext.CompanyProfiles.AsNoTracking()
                        on ticket.CompanyProfileId equals company.Id
                    group ticket by new { company.Id, company.CompanyName }
                    into groupTickets
                    select new TicketsByCompanyReportRowDto
                    {
                        CompanyProfileId = groupTickets.Key.Id,
                        CompanyName = groupTickets.Key.CompanyName,
                        TotalTickets = groupTickets.Count(),
                        OpenTickets = groupTickets.Count(x => x.Status == TicketStatus.Open),
                        InProgressTickets = groupTickets.Count(x => x.Status == TicketStatus.InProgress),
                        WaitingTickets = groupTickets.Count(x => x.Status == TicketStatus.WaitingForCustomer),
                        ResolvedTickets = groupTickets.Count(x => x.Status == TicketStatus.Resolved),
                        ClosedTickets = groupTickets.Count(x => x.Status == TicketStatus.Closed)
                    };

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.CompanyName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<TicketsByCompanyReportRowDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }

    [HttpGet("sla-breaches")]
    public async Task<ActionResult<PagedResult<SlaBreachReportRowDto>>> GetSlaBreaches(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var firstResponseQuery = from ticket in _dbContext.Tickets.AsNoTracking()
                                 where ticket.FirstResponseBreached
                                 join company in _dbContext.CompanyProfiles.AsNoTracking()
                                     on ticket.CompanyProfileId equals company.Id
                                 join service in _dbContext.Services.AsNoTracking()
                                     on ticket.ServiceCatalogItemId equals service.Id into serviceJoin
                                 from service in serviceJoin.DefaultIfEmpty()
                                 select new SlaBreachReportRowDto
                                 {
                                     TicketId = ticket.Id,
                                     TicketTitle = ticket.Title,
                                     CompanyName = company.CompanyName,
                                     ServiceName = service != null ? service.Name : null,
                                     BreachType = "FirstResponse",
                                     BreachedAtUtc = ticket.FirstResponseDueAtUtc
                                 };

        var resolutionQuery = from ticket in _dbContext.Tickets.AsNoTracking()
                              where ticket.ResolutionBreached
                              join company in _dbContext.CompanyProfiles.AsNoTracking()
                                  on ticket.CompanyProfileId equals company.Id
                              join service in _dbContext.Services.AsNoTracking()
                                  on ticket.ServiceCatalogItemId equals service.Id into serviceJoin
                              from service in serviceJoin.DefaultIfEmpty()
                              select new SlaBreachReportRowDto
                              {
                                  TicketId = ticket.Id,
                                  TicketTitle = ticket.Title,
                                  CompanyName = company.CompanyName,
                                  ServiceName = service != null ? service.Name : null,
                                  BreachType = "Resolution",
                                  BreachedAtUtc = ticket.ResolutionDueAtUtc
                              };

        var query = firstResponseQuery.Union(resolutionQuery);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.BreachedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<SlaBreachReportRowDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }

    [HttpGet("expert-performance")]
    public async Task<ActionResult<PagedResult<ExpertPerformanceReportRowDto>>> GetExpertPerformance(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var ticketStatsQuery = _dbContext.Tickets
            .AsNoTracking()
            .Where(x => x.AssignedExpertProfileId.HasValue)
            .GroupBy(x => x.AssignedExpertProfileId!.Value)
            .Select(groupTickets => new
            {
                ExpertProfileId = groupTickets.Key,
                AssignedTickets = groupTickets.Count(),
                ResolvedTickets = groupTickets.Count(x => x.Status == TicketStatus.Resolved || x.Status == TicketStatus.Closed),
                AverageFirstResponseMinutes = groupTickets
                    .Where(x => x.FirstResponseAtUtc.HasValue)
                    .Average(x => (double?)EF.Functions.DateDiffMinute(x.CreatedAtUtc, x.FirstResponseAtUtc!.Value)),
                AverageResolutionMinutes = groupTickets
                    .Where(x => x.ResolvedAtUtc.HasValue)
                    .Average(x => (double?)EF.Functions.DateDiffMinute(x.CreatedAtUtc, x.ResolvedAtUtc!.Value)),
                FirstResponseBreaches = groupTickets.Count(x => x.FirstResponseBreached),
                ResolutionBreaches = groupTickets.Count(x => x.ResolutionBreached)
            });

        var satisfactionStatsQuery = from satisfaction in _dbContext.TicketSatisfactions.AsNoTracking()
                                     join ticket in _dbContext.Tickets.AsNoTracking()
                                         on satisfaction.TicketId equals ticket.Id
                                     where ticket.AssignedExpertProfileId.HasValue
                                     group satisfaction by ticket.AssignedExpertProfileId!.Value
            into groupSatisfaction
                                     select new
                                     {
                                         ExpertProfileId = groupSatisfaction.Key,
                                         AverageRating = groupSatisfaction.Average(x => (double?)x.Rating)
                                     };

        var query = from expert in _dbContext.ExpertProfiles.AsNoTracking()
                    join ticketStats in ticketStatsQuery
                        on expert.Id equals ticketStats.ExpertProfileId into ticketStatsJoin
                    from ticketStats in ticketStatsJoin.DefaultIfEmpty()
                    join satisfactionStats in satisfactionStatsQuery
                        on expert.Id equals satisfactionStats.ExpertProfileId into satisfactionJoin
                    from satisfactionStats in satisfactionJoin.DefaultIfEmpty()
                    select new ExpertPerformanceReportRowDto
                    {
                        ExpertProfileId = expert.Id,
                        ExpertName = expert.FullName,
                        AssignedTickets = ticketStats != null ? ticketStats.AssignedTickets : 0,
                        ResolvedTickets = ticketStats != null ? ticketStats.ResolvedTickets : 0,
                        AverageFirstResponseMinutes = ticketStats != null ? ticketStats.AverageFirstResponseMinutes : null,
                        AverageResolutionMinutes = ticketStats != null ? ticketStats.AverageResolutionMinutes : null,
                        FirstResponseBreaches = ticketStats != null ? ticketStats.FirstResponseBreaches : 0,
                        ResolutionBreaches = ticketStats != null ? ticketStats.ResolutionBreaches : 0,
                        AverageRating = satisfactionStats != null ? satisfactionStats.AverageRating : null
                    };

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.ExpertName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<ExpertPerformanceReportRowDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }

    [HttpGet("top-services")]
    public async Task<ActionResult<PagedResult<TopServiceReportRowDto>>> GetTopServices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = from ticket in _dbContext.Tickets.AsNoTracking()
                    where ticket.ServiceCatalogItemId.HasValue
                    join service in _dbContext.Services.AsNoTracking()
                        on ticket.ServiceCatalogItemId equals service.Id
                    group ticket by new { service.Id, service.Name }
                    into groupTickets
                    select new TopServiceReportRowDto
                    {
                        ServiceId = groupTickets.Key.Id,
                        ServiceName = groupTickets.Key.Name,
                        TicketCount = groupTickets.Count(),
                        AverageResolutionMinutes = groupTickets
                            .Where(x => x.ResolvedAtUtc.HasValue)
                            .Average(x => (double?)EF.Functions.DateDiffMinute(x.CreatedAtUtc, x.ResolvedAtUtc!.Value)),
                        BreachCount = groupTickets.Count(x => x.FirstResponseBreached || x.ResolutionBreached)
                    };

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.TicketCount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<TopServiceReportRowDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }
}
