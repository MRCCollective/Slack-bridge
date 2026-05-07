using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;

namespace SlackBridge.Web.Services;

public interface IUsageService
{
    Task<UsageSnapshot> GetCurrentAsync(CancellationToken cancellationToken);
    Task EnsureEventLimitAsync(CancellationToken cancellationToken);
    Task IncrementEventsAsync(CancellationToken cancellationToken);
    Task EnsureProjectLimitAsync(CancellationToken cancellationToken);
    Task EnsureApiKeyLimitAsync(CancellationToken cancellationToken);
}

public sealed record UsageSnapshot(
    PlanType Plan,
    PlanLimitSet Limits,
    int EventsSentThisMonth,
    int ProjectCount,
    int ApiKeyCount);

public sealed class UsageService(
    SlackBridgeDbContext dbContext,
    ICustomerInstanceContext customerInstanceContext,
    IPlanLimitService planLimitService) : IUsageService
{
    public async Task<UsageSnapshot> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var customerInstance = await customerInstanceContext.GetAsync(cancellationToken);
        var plan = customerInstance.Subscription?.Plan ?? PlanType.Free;
        var limits = planLimitService.GetLimits(plan);

        var eventsSent = await dbContext.UsageMetrics
            .Where(metric =>
                metric.CustomerInstanceId == customerInstanceContext.CustomerInstanceId &&
                metric.Year == now.Year &&
                metric.Month == now.Month)
            .Select(metric => metric.EventsSent)
            .SingleOrDefaultAsync(cancellationToken);

        var projects = await dbContext.Projects
            .CountAsync(project => project.CustomerInstanceId == customerInstanceContext.CustomerInstanceId, cancellationToken);

        var apiKeys = await dbContext.ApiKeys
            .CountAsync(apiKey => apiKey.CustomerInstanceId == customerInstanceContext.CustomerInstanceId, cancellationToken);

        return new UsageSnapshot(plan, limits, eventsSent, projects, apiKeys);
    }

    public async Task EnsureEventLimitAsync(CancellationToken cancellationToken)
    {
        var snapshot = await GetCurrentAsync(cancellationToken);
        if (snapshot.EventsSentThisMonth >= snapshot.Limits.EventsPerMonth)
        {
            throw new PlanLimitExceededException("Monthly event limit exceeded.");
        }
    }

    public async Task IncrementEventsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var metric = await dbContext.UsageMetrics.SingleOrDefaultAsync(metric =>
            metric.CustomerInstanceId == customerInstanceContext.CustomerInstanceId &&
            metric.Year == now.Year &&
            metric.Month == now.Month,
            cancellationToken);

        if (metric is null)
        {
            metric = new UsageMetric
            {
                CustomerInstanceId = customerInstanceContext.CustomerInstanceId,
                Year = now.Year,
                Month = now.Month
            };
            dbContext.UsageMetrics.Add(metric);
        }

        metric.EventsSent++;
        metric.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureProjectLimitAsync(CancellationToken cancellationToken)
    {
        var snapshot = await GetCurrentAsync(cancellationToken);
        if (snapshot.ProjectCount >= snapshot.Limits.Projects)
        {
            throw new PlanLimitExceededException("Project limit exceeded for the current plan.");
        }
    }

    public async Task EnsureApiKeyLimitAsync(CancellationToken cancellationToken)
    {
        var snapshot = await GetCurrentAsync(cancellationToken);
        if (snapshot.ApiKeyCount >= snapshot.Limits.ApiKeys)
        {
            throw new PlanLimitExceededException("API key limit exceeded for the current plan.");
        }
    }
}

public sealed class PlanLimitExceededException(string message) : Exception(message);
