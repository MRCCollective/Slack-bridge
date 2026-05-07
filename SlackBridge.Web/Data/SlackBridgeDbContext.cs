using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Models;

namespace SlackBridge.Web.Data;

public sealed class SlackBridgeDbContext(DbContextOptions<SlackBridgeDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<CustomerInstance> CustomerInstances => Set<CustomerInstance>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<UsageMetric> UsageMetrics => Set<UsageMetric>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<EventDefinition> EventDefinitions => Set<EventDefinition>();
    public DbSet<EventLog> EventLogs => Set<EventLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustomerInstance>()
            .HasIndex(instance => instance.CompanyName);

        modelBuilder.Entity<CustomerInstance>()
            .HasData(new CustomerInstance
            {
                Id = 1,
                CompanyName = "Workspace",
                CreatedAtUtc = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero)
            });

        modelBuilder.Entity<Subscription>()
            .HasIndex(subscription => subscription.CustomerInstanceId)
            .IsUnique();

        modelBuilder.Entity<Subscription>()
            .HasData(new Subscription
            {
                Id = 1,
                CustomerInstanceId = 1,
                Plan = PlanType.Free,
                Status = "active",
                UpdatedAtUtc = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero)
            });

        modelBuilder.Entity<UsageMetric>()
            .HasIndex(metric => new { metric.CustomerInstanceId, metric.Year, metric.Month })
            .IsUnique();

        modelBuilder.Entity<Project>()
            .HasIndex(project => new { project.CustomerInstanceId, project.Name })
            .IsUnique();

        modelBuilder.Entity<Project>()
            .HasOne(project => project.CustomerInstance)
            .WithMany()
            .HasForeignKey(project => project.CustomerInstanceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApiKey>()
            .HasIndex(apiKey => apiKey.KeyHash)
            .IsUnique();

        modelBuilder.Entity<ApiKey>()
            .HasOne(apiKey => apiKey.CustomerInstance)
            .WithMany()
            .HasForeignKey(apiKey => apiKey.CustomerInstanceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApiKey>()
            .HasOne(apiKey => apiKey.Project)
            .WithMany(project => project.ApiKeys)
            .HasForeignKey(apiKey => apiKey.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventDefinition>()
            .HasIndex(definition => new { definition.ProjectId, definition.Key })
            .IsUnique();

        modelBuilder.Entity<EventDefinition>()
            .HasOne(definition => definition.CustomerInstance)
            .WithMany()
            .HasForeignKey(definition => definition.CustomerInstanceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventDefinition>()
            .HasOne(definition => definition.Project)
            .WithMany(project => project.EventDefinitions)
            .HasForeignKey(definition => definition.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventLog>()
            .Property(log => log.EventKey)
            .HasMaxLength(120);

        modelBuilder.Entity<EventLog>()
            .HasOne(log => log.CustomerInstance)
            .WithMany()
            .HasForeignKey(log => log.CustomerInstanceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventLog>()
            .HasIndex(log => log.CreatedAtUtc);

        modelBuilder.Entity<EventLog>()
            .HasOne(log => log.Project)
            .WithMany()
            .HasForeignKey(log => log.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventLog>()
            .HasOne(log => log.EventDefinition)
            .WithMany(definition => definition.EventLogs)
            .HasForeignKey(log => log.EventDefinitionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(user => user.CustomerInstance)
            .WithMany(instance => instance.Users)
            .HasForeignKey(user => user.CustomerInstanceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
