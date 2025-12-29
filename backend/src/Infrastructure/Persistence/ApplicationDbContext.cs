using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ConfigureProfiles(builder);
        ConfigureSkills(builder);
        ConfigureServices(builder);
        ConfigureAssets(builder);
        ConfigureContracts(builder);
        ConfigureTickets(builder);
        builder.ApplySoftDeleteQueryFilters();
    }

    public DbSet<ExpertProfile> ExpertProfiles => Set<ExpertProfile>();
    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<ExpertSkill> ExpertSkills => Set<ExpertSkill>();
    public DbSet<CompanyExpertLink> CompanyExpertLinks => Set<CompanyExpertLink>();
    public DbSet<ServiceCatalogItem> Services => Set<ServiceCatalogItem>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractService> ContractServices => Set<ContractService>();
    public DbSet<ContractAsset> ContractAssets => Set<ContractAsset>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<TicketTimeLog> TicketTimeLogs => Set<TicketTimeLog>();
    public DbSet<TicketSatisfaction> TicketSatisfactions => Set<TicketSatisfaction>();

    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInfo();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditInfo()
    {
        var now = DateTime.UtcNow;
        var userId = _currentUserService.UserId;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAtUtc == default)
                {
                    entry.Entity.CreatedAtUtc = now;
                }

                if (string.IsNullOrWhiteSpace(entry.Entity.CreatedBy))
                {
                    entry.Entity.CreatedBy = userId;
                }

                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedBy = userId ?? entry.Entity.CreatedBy;
                entry.Entity.IsDeleted = false;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedBy = userId;

                entry.Property(p => p.CreatedAtUtc).IsModified = false;
                entry.Property(p => p.CreatedBy).IsModified = false;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedBy = userId;

                entry.Property(p => p.CreatedAtUtc).IsModified = false;
                entry.Property(p => p.CreatedBy).IsModified = false;
            }
        }
    }

    private static void ConfigureProfiles(ModelBuilder builder)
    {
        builder.Entity<ExpertProfile>(entity =>
        {
            entity.Property(p => p.FullName).HasMaxLength(200).IsRequired();
            entity.HasIndex(p => p.UserId).IsUnique();
        });

        builder.Entity<CompanyProfile>(entity =>
        {
            entity.Property(p => p.CompanyName).HasMaxLength(200).IsRequired();
            entity.HasIndex(p => p.UserId).IsUnique();
        });
    }

    private static void ConfigureSkills(ModelBuilder builder)
    {
        builder.Entity<Skill>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.HasIndex(p => p.Name);
        });

        builder.Entity<ExpertSkill>(entity =>
        {
            entity.HasIndex(p => new { p.ExpertProfileId, p.SkillId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });

        builder.Entity<CompanyExpertLink>(entity =>
        {
            entity.HasIndex(p => new { p.CompanyProfileId, p.ExpertProfileId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(p => new { p.CompanyProfileId, p.IsPrimary })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0 AND [IsPrimary] = 1");
        });
    }

    private static void ConfigureServices(ModelBuilder builder)
    {
        builder.Entity<ServiceCatalogItem>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.HasIndex(p => p.Name);
            entity.HasIndex(p => p.IsActive);
        });
    }

    private static void ConfigureAssets(ModelBuilder builder)
    {
        builder.Entity<Asset>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.AssetTag).HasMaxLength(100);
            entity.Property(p => p.SerialNumber).HasMaxLength(100);
            entity.Property(p => p.Notes).HasMaxLength(1000);
            entity.HasIndex(p => p.CompanyProfileId);
            entity.HasIndex(p => p.Name);
            entity.HasIndex(p => p.AssetTag);
            entity.HasIndex(p => p.PrimaryExpertProfileId);
        });
    }

    private static void ConfigureContracts(ModelBuilder builder)
    {
        builder.Entity<Contract>(entity =>
        {
            entity.Property(p => p.MonthlyPrice).HasColumnType("decimal(18,2)");
            entity.HasIndex(p => p.CompanyProfileId);
            entity.HasIndex(p => p.IsActive);
        });

        builder.Entity<ContractService>(entity =>
        {
            entity.HasIndex(p => new { p.ContractId, p.ServiceCatalogItemId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(p => p.ServiceCatalogItemId);
        });

        builder.Entity<ContractAsset>(entity =>
        {
            entity.HasIndex(p => new { p.ContractId, p.AssetId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(p => p.AssetId);
        });
    }

    private static void ConfigureTickets(ModelBuilder builder)
    {
        builder.Entity<Ticket>(entity =>
        {
            entity.Property(p => p.Title).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(4000).IsRequired();
            entity.HasIndex(p => p.CompanyProfileId);
            entity.HasIndex(p => p.AssignedExpertProfileId);
            entity.HasIndex(p => p.Status);
            entity.HasIndex(p => p.ServiceCatalogItemId);
            entity.HasIndex(p => p.AssetId);
            entity.HasIndex(p => p.CreatedAtUtc);
        });

        builder.Entity<TicketMessage>(entity =>
        {
            entity.Property(p => p.Body).HasMaxLength(4000).IsRequired();
            entity.HasIndex(p => p.TicketId);
            entity.HasIndex(p => p.CreatedAtUtc);
            entity.HasIndex(p => p.IsInternal);
        });

        builder.Entity<TicketTimeLog>(entity =>
        {
            entity.HasIndex(p => p.TicketId);
            entity.HasIndex(p => p.ExpertProfileId);
            entity.HasIndex(p => p.LoggedAtUtc);
        });

        builder.Entity<TicketSatisfaction>(entity =>
        {
            entity.HasIndex(p => p.TicketId)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(p => p.CompanyProfileId);
            entity.HasIndex(p => p.Rating);
            entity.Property(p => p.Comment).HasMaxLength(2000);
        });
    }
}
