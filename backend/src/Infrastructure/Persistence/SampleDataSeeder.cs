using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public static class SampleDataSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        var now = DateTime.UtcNow;

        await EnsureRolesAsync(roleManager);
        var skills = await EnsureSkillsAsync(dbContext, now);
        var services = await EnsureServicesAsync(dbContext, now);

        var companies = await EnsureCompanyProfilesAsync(dbContext, userManager, now);
        var experts = await EnsureExpertProfilesAsync(dbContext, userManager, now);

        await EnsureCompanyExpertLinksAsync(dbContext, companies, experts, now);
        await EnsureExpertSkillsAsync(dbContext, experts, skills, now);
        var assets = await EnsureAssetsAsync(dbContext, companies, experts, now);
        var contracts = await EnsureContractsAsync(dbContext, companies, services, assets, now);
        await EnsureTicketsAsync(dbContext, companies, experts, services, assets, contracts, now);
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = new[] { RoleNames.Admin, RoleNames.Expert, RoleNames.Company };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task<List<Skill>> EnsureSkillsAsync(ApplicationDbContext dbContext, DateTime now)
    {
        var skillSeeds = new[]
        {
            ("Windows Server", "Windows Server administration and maintenance."),
            ("Networking", "LAN/WAN routing, switching, and troubleshooting."),
            ("SQL Server", "SQL Server performance and backups."),
            ("Active Directory", "AD user/group policy management."),
            ("Azure AD", "Identity synchronization and access management."),
            ("Backup & Recovery", "Backup systems and restore validation."),
            ("Security Hardening", "Endpoint and server hardening."),
            ("Virtualization", "Hyper-V and virtualization platforms."),
            ("Endpoint Management", "Device lifecycle and patching."),
            ("Monitoring", "Alerts, dashboards, and SLA monitoring.")
        };

        foreach (var (name, description) in skillSeeds)
        {
            var exists = await dbContext.Skills.AnyAsync(skill => skill.Name == name);
            if (!exists)
            {
                dbContext.Skills.Add(new Skill
                {
                    Name = name,
                    Description = description,
                    CreatedAtUtc = now.AddDays(-30),
                    CreatedBy = "seed"
                });
            }
        }

        await dbContext.SaveChangesAsync();
        return await dbContext.Skills.OrderBy(skill => skill.Name).Take(10).ToListAsync();
    }

    private static async Task<List<ServiceCatalogItem>> EnsureServicesAsync(ApplicationDbContext dbContext, DateTime now)
    {
        var serviceSeeds = new[]
        {
            ("Managed Endpoint Support", "Workstation and laptop support.", 60, 480),
            ("Server Maintenance", "Patching, updates, and health checks.", 45, 360),
            ("Network Monitoring", "Proactive network monitoring.", 30, 240),
            ("Backup Management", "Backup job monitoring and restores.", 60, 600),
            ("Security Response", "Security incident response.", 15, 180),
            ("Email Support", "Mail flow and mailbox management.", 60, 300),
            ("Database Support", "SQL Server maintenance and tuning.", 45, 360),
            ("VoIP Support", "Telephony configuration and support.", 60, 480),
            ("Cloud Administration", "Azure and cloud tenant management.", 60, 480),
            ("Onsite Assistance", "Scheduled onsite maintenance.", 120, 1440)
        };

        foreach (var (name, description, fr, res) in serviceSeeds)
        {
            var exists = await dbContext.Services.AnyAsync(service => service.Name == name);
            if (!exists)
            {
                dbContext.Services.Add(new ServiceCatalogItem
                {
                    Name = name,
                    Description = description,
                    DefaultFirstResponseMinutes = fr,
                    DefaultResolutionMinutes = res,
                    IsActive = true,
                    CreatedAtUtc = now.AddDays(-25),
                    CreatedBy = "seed"
                });
            }
        }

        await dbContext.SaveChangesAsync();
        return await dbContext.Services.OrderBy(service => service.Name).Take(10).ToListAsync();
    }

    private static async Task<List<CompanyProfile>> EnsureCompanyProfilesAsync(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        DateTime now)
    {
        var companies = new List<CompanyProfile>();
        for (var i = 1; i <= 10; i++)
        {
            var email = $"company{i:00}@msp.local";
            var user = await EnsureUserAsync(userManager, email, "Company!23456", now.AddDays(-20));
            if (user is null)
            {
                continue;
            }

            if (!await userManager.IsInRoleAsync(user, RoleNames.Company))
            {
                await userManager.AddToRoleAsync(user, RoleNames.Company);
            }

            var profile = await dbContext.CompanyProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile is null)
            {
                profile = new CompanyProfile
                {
                    UserId = user.Id,
                    CompanyName = $"Contoso {i:00}",
                    CreatedAtUtc = now.AddDays(-20 + i),
                    CreatedBy = "seed"
                };
                dbContext.CompanyProfiles.Add(profile);
            }

            companies.Add(profile);
        }

        await dbContext.SaveChangesAsync();
        return companies;
    }

    private static async Task<List<ExpertProfile>> EnsureExpertProfilesAsync(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        DateTime now)
    {
        var experts = new List<ExpertProfile>();
        for (var i = 1; i <= 10; i++)
        {
            var email = $"expert{i:00}@msp.local";
            var user = await EnsureUserAsync(userManager, email, "Expert!23456", now.AddDays(-22));
            if (user is null)
            {
                continue;
            }

            if (!await userManager.IsInRoleAsync(user, RoleNames.Expert))
            {
                await userManager.AddToRoleAsync(user, RoleNames.Expert);
            }

            var profile = await dbContext.ExpertProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile is null)
            {
                profile = new ExpertProfile
                {
                    UserId = user.Id,
                    FullName = $"Expert {i:00}",
                    IsApproved = true,
                    CreatedAtUtc = now.AddDays(-22 + i),
                    CreatedBy = "seed"
                };
                dbContext.ExpertProfiles.Add(profile);
            }

            experts.Add(profile);
        }

        await dbContext.SaveChangesAsync();
        return experts;
    }

    private static async Task EnsureCompanyExpertLinksAsync(
        ApplicationDbContext dbContext,
        IReadOnlyList<CompanyProfile> companies,
        IReadOnlyList<ExpertProfile> experts,
        DateTime now)
    {
        for (var i = 0; i < Math.Min(companies.Count, experts.Count); i++)
        {
            var company = companies[i];
            var expert = experts[i];

            var exists = await dbContext.CompanyExpertLinks.AnyAsync(link =>
                link.CompanyProfileId == company.Id && link.ExpertProfileId == expert.Id);

            if (!exists)
            {
                dbContext.CompanyExpertLinks.Add(new CompanyExpertLink
                {
                    CompanyProfileId = company.Id,
                    ExpertProfileId = expert.Id,
                    IsPrimary = true,
                    CreatedAtUtc = now.AddDays(-10),
                    CreatedBy = "seed"
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureExpertSkillsAsync(
        ApplicationDbContext dbContext,
        IReadOnlyList<ExpertProfile> experts,
        IReadOnlyList<Skill> skills,
        DateTime now)
    {
        for (var i = 0; i < experts.Count; i++)
        {
            var expert = experts[i];
            var skillA = skills[i % skills.Count];
            var skillB = skills[(i + 1) % skills.Count];

            await EnsureExpertSkillAsync(dbContext, expert, skillA, now);
            await EnsureExpertSkillAsync(dbContext, expert, skillB, now);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureExpertSkillAsync(
        ApplicationDbContext dbContext,
        ExpertProfile expert,
        Skill skill,
        DateTime now)
    {
        var exists = await dbContext.ExpertSkills.AnyAsync(es =>
            es.ExpertProfileId == expert.Id && es.SkillId == skill.Id);
        if (!exists)
        {
            dbContext.ExpertSkills.Add(new ExpertSkill
            {
                ExpertProfileId = expert.Id,
                SkillId = skill.Id,
                CreatedAtUtc = now.AddDays(-12),
                CreatedBy = "seed"
            });
        }
    }

    private static async Task<List<Asset>> EnsureAssetsAsync(
        ApplicationDbContext dbContext,
        IReadOnlyList<CompanyProfile> companies,
        IReadOnlyList<ExpertProfile> experts,
        DateTime now)
    {
        var assets = new List<Asset>();
        for (var i = 0; i < companies.Count; i++)
        {
            var company = companies[i];
            var expert = experts.Count > i ? experts[i] : null;
            var assetName = $"Asset {i + 1:00} - Workstation";

            var existing = await dbContext.Assets.FirstOrDefaultAsync(asset =>
                asset.CompanyProfileId == company.Id && asset.Name == assetName);
            if (existing is null)
            {
                existing = new Asset
                {
                    CompanyProfileId = company.Id,
                    PrimaryExpertProfileId = (i % 2 == 0) ? expert?.Id : null,
                    Name = assetName,
                    AssetTag = $"ASSET-{i + 1:000}",
                    SerialNumber = $"SN-{i + 1:000}",
                    Notes = "Seeded asset.",
                    CreatedAtUtc = now.AddDays(-9),
                    CreatedBy = "seed"
                };
                dbContext.Assets.Add(existing);
            }

            assets.Add(existing);
        }

        await dbContext.SaveChangesAsync();
        return assets;
    }

    private static async Task<List<Contract>> EnsureContractsAsync(
        ApplicationDbContext dbContext,
        IReadOnlyList<CompanyProfile> companies,
        IReadOnlyList<ServiceCatalogItem> services,
        IReadOnlyList<Asset> assets,
        DateTime now)
    {
        var contracts = new List<Contract>();
        for (var i = 0; i < companies.Count; i++)
        {
            var company = companies[i];
            var existing = await dbContext.Contracts.FirstOrDefaultAsync(contract =>
                contract.CompanyProfileId == company.Id && contract.IsActive);
            if (existing is null)
            {
                existing = new Contract
                {
                    CompanyProfileId = company.Id,
                    MonthlyPrice = 1000 + (i * 100),
                    IncludedSupportMinutesPerMonth = 600,
                    OnsiteDaysIncluded = 2,
                    IsActive = true,
                    CreatedAtUtc = now.AddDays(-8),
                    CreatedBy = "seed"
                };
                dbContext.Contracts.Add(existing);
            }

            contracts.Add(existing);
        }

        await dbContext.SaveChangesAsync();

        for (var i = 0; i < contracts.Count; i++)
        {
            var contract = contracts[i];
            var service = services[i % services.Count];
            var asset = assets[i % assets.Count];

            var serviceExists = await dbContext.ContractServices.AnyAsync(link =>
                link.ContractId == contract.Id && link.ServiceCatalogItemId == service.Id);
            if (!serviceExists)
            {
                dbContext.ContractServices.Add(new ContractService
                {
                    ContractId = contract.Id,
                    ServiceCatalogItemId = service.Id,
                    CustomFirstResponseMinutes = (i % 2 == 0) ? 30 : null,
                    CustomResolutionMinutes = (i % 2 == 0) ? 240 : null,
                    CreatedAtUtc = now.AddDays(-7),
                    CreatedBy = "seed"
                });
            }

            var assetExists = await dbContext.ContractAssets.AnyAsync(link =>
                link.ContractId == contract.Id && link.AssetId == asset.Id);
            if (!assetExists)
            {
                dbContext.ContractAssets.Add(new ContractAsset
                {
                    ContractId = contract.Id,
                    AssetId = asset.Id,
                    CreatedAtUtc = now.AddDays(-7),
                    CreatedBy = "seed"
                });
            }
        }

        await dbContext.SaveChangesAsync();
        return contracts;
    }

    private static async Task EnsureTicketsAsync(
        ApplicationDbContext dbContext,
        IReadOnlyList<CompanyProfile> companies,
        IReadOnlyList<ExpertProfile> experts,
        IReadOnlyList<ServiceCatalogItem> services,
        IReadOnlyList<Asset> assets,
        IReadOnlyList<Contract> contracts,
        DateTime now)
    {
        for (var i = 0; i < 10; i++)
        {
            var title = $"Seed Ticket {i + 1:00}";
            var existing = await dbContext.Tickets.FirstOrDefaultAsync(ticket => ticket.Title == title);
            if (existing is not null)
            {
                continue;
            }

            var company = companies[i % companies.Count];
            var expert = experts[i % experts.Count];
            var service = services[i % services.Count];
            var asset = assets[i % assets.Count];
            var contract = contracts[i % contracts.Count];

            var contractService = await dbContext.ContractServices
                .FirstOrDefaultAsync(cs => cs.ContractId == contract.Id && cs.ServiceCatalogItemId == service.Id);

            var slaFirst = contractService?.CustomFirstResponseMinutes
                           ?? service.DefaultFirstResponseMinutes;
            var slaResolution = contractService?.CustomResolutionMinutes
                               ?? service.DefaultResolutionMinutes;

            var createdAt = now.AddDays(-5).AddHours(i);
            var firstDue = createdAt.AddMinutes(slaFirst);
            var resolutionDue = createdAt.AddMinutes(slaResolution);

            var status = i switch
            {
                <= 2 => TicketStatus.Closed,
                <= 4 => TicketStatus.Resolved,
                <= 6 => TicketStatus.InProgress,
                <= 8 => TicketStatus.WaitingForCustomer,
                _ => TicketStatus.Open
            };

            var ticket = new Ticket
            {
                CompanyProfileId = company.Id,
                ServiceCatalogItemId = service.Id,
                AssetId = asset.Id,
                AssignedExpertProfileId = expert.Id,
                Title = title,
                Description = $"Seeded ticket description {i + 1:00}.",
                Status = status,
                SlaFirstResponseMinutes = slaFirst,
                SlaResolutionMinutes = slaResolution,
                FirstResponseDueAtUtc = firstDue,
                ResolutionDueAtUtc = resolutionDue,
                CreatedAtUtc = createdAt,
                CreatedBy = "seed"
            };

            var firstResponseAt = createdAt.AddMinutes(30);
            if (i == 1)
            {
                firstResponseAt = firstDue.AddMinutes(10);
                ticket.FirstResponseBreached = true;
            }

            ticket.FirstResponseAtUtc = firstResponseAt;

            if (status is TicketStatus.Resolved or TicketStatus.Closed)
            {
                var resolvedAt = createdAt.AddMinutes(slaResolution - 30);
                if (i == 2)
                {
                    resolvedAt = resolutionDue.AddMinutes(30);
                    ticket.ResolutionBreached = true;
                }

                ticket.ResolvedAtUtc = resolvedAt;
                if (status == TicketStatus.Closed)
                {
                    ticket.ClosedAtUtc = resolvedAt.AddHours(1);
                }
            }

            dbContext.Tickets.Add(ticket);
            await dbContext.SaveChangesAsync();

            await EnsureTicketMessagesAsync(dbContext, ticket, company, expert, now);
            await EnsureTicketTimeLogAsync(dbContext, ticket, expert, now);
            await EnsureTicketSatisfactionAsync(dbContext, ticket, company, now);
        }
    }

    private static async Task EnsureTicketMessagesAsync(
        ApplicationDbContext dbContext,
        Ticket ticket,
        CompanyProfile company,
        ExpertProfile expert,
        DateTime now)
    {
        var companyMessageExists = await dbContext.TicketMessages.AnyAsync(message =>
            message.TicketId == ticket.Id && message.AuthorRole == TicketMessageAuthorRole.Company);
        if (!companyMessageExists)
        {
            dbContext.TicketMessages.Add(new TicketMessage
            {
                TicketId = ticket.Id,
                AuthorUserId = company.UserId,
                AuthorRole = TicketMessageAuthorRole.Company,
                Body = "We are seeing an issue and need help.",
                IsInternal = false,
                CreatedAtUtc = ticket.CreatedAtUtc.AddMinutes(5),
                CreatedBy = "seed"
            });
        }

        var expertMessageExists = await dbContext.TicketMessages.AnyAsync(message =>
            message.TicketId == ticket.Id && message.AuthorRole == TicketMessageAuthorRole.Expert);
        if (!expertMessageExists)
        {
            dbContext.TicketMessages.Add(new TicketMessage
            {
                TicketId = ticket.Id,
                AuthorUserId = expert.UserId,
                AuthorRole = TicketMessageAuthorRole.Expert,
                Body = "Acknowledged. We are investigating.",
                IsInternal = false,
                CreatedAtUtc = ticket.CreatedAtUtc.AddMinutes(15),
                CreatedBy = "seed"
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureTicketTimeLogAsync(
        ApplicationDbContext dbContext,
        Ticket ticket,
        ExpertProfile expert,
        DateTime now)
    {
        var exists = await dbContext.TicketTimeLogs.AnyAsync(log => log.TicketId == ticket.Id);
        if (exists)
        {
            return;
        }

        dbContext.TicketTimeLogs.Add(new TicketTimeLog
        {
            TicketId = ticket.Id,
            ExpertProfileId = expert.Id,
            Minutes = 60,
            WorkType = TicketWorkType.Remote,
            LoggedAtUtc = ticket.CreatedAtUtc.AddHours(2),
            CreatedAtUtc = now,
            CreatedBy = "seed"
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureTicketSatisfactionAsync(
        ApplicationDbContext dbContext,
        Ticket ticket,
        CompanyProfile company,
        DateTime now)
    {
        if (ticket.Status != TicketStatus.Closed)
        {
            return;
        }

        var exists = await dbContext.TicketSatisfactions.AnyAsync(s =>
            s.TicketId == ticket.Id && s.CompanyProfileId == company.Id);
        if (exists)
        {
            return;
        }

        dbContext.TicketSatisfactions.Add(new TicketSatisfaction
        {
            TicketId = ticket.Id,
            CompanyProfileId = company.Id,
            Rating = 4,
            ResponseTimeRating = 4,
            ResolutionQualityRating = 5,
            CommunicationRating = 4,
            Comment = "Good support from the team.",
            CreatedAtUtc = now,
            CreatedBy = "seed"
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<ApplicationUser?> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        DateTime createdAt)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return user;
        }

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            CreatedAtUtc = createdAt,
            CreatedBy = "seed"
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return null;
        }

        return user;
    }
}
