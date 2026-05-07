using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;

namespace SlackBridge.Web.Services;

public interface IBillingService
{
    Task<Subscription> GetSubscriptionAsync(CancellationToken cancellationToken);
    Task<Subscription> ChangePlanAsync(PlanType plan, CancellationToken cancellationToken);
}

public sealed class BillingService(
    SlackBridgeDbContext dbContext,
    ICustomerInstanceContext customerInstanceContext) : IBillingService
{
    public async Task<Subscription> GetSubscriptionAsync(CancellationToken cancellationToken) =>
        await dbContext.Subscriptions.SingleAsync(
            subscription => subscription.CustomerInstanceId == customerInstanceContext.CustomerInstanceId,
            cancellationToken);

    public async Task<Subscription> ChangePlanAsync(PlanType plan, CancellationToken cancellationToken)
    {
        var subscription = await GetSubscriptionAsync(cancellationToken);
        subscription.Plan = plan;
        subscription.Status = "active";
        subscription.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return subscription;
    }
}
