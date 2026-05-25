using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using System.Security.Claims;

namespace SlackBridge.Web.Services;

public interface ICustomerInstanceContext
{
    int CustomerInstanceId { get; }
    Task<CustomerInstance> GetAsync(CancellationToken cancellationToken);
}

public sealed class CustomerInstanceContext(
    SlackBridgeDbContext dbContext,
    IHttpContextAccessor httpContextAccessor) : ICustomerInstanceContext
{
    public const string CustomerInstanceIdClaimType = "customer_instance_id";

    public int CustomerInstanceId =>
        int.TryParse(
            httpContextAccessor.HttpContext?.User.FindFirstValue(CustomerInstanceIdClaimType),
            out var customerInstanceId)
            ? customerInstanceId
            : 1;

    public async Task<CustomerInstance> GetAsync(CancellationToken cancellationToken) =>
        await dbContext.CustomerInstances
            .Include(instance => instance.Subscription)
            .SingleAsync(instance => instance.Id == CustomerInstanceId, cancellationToken);
}
