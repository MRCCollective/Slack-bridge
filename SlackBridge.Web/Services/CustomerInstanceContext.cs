using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;

namespace SlackBridge.Web.Services;

public interface ICustomerInstanceContext
{
    int CustomerInstanceId { get; }
    Task<CustomerInstance> GetAsync(CancellationToken cancellationToken);
}

public sealed class CustomerInstanceContext(SlackBridgeDbContext dbContext) : ICustomerInstanceContext
{
    public int CustomerInstanceId => 1;

    public async Task<CustomerInstance> GetAsync(CancellationToken cancellationToken) =>
        await dbContext.CustomerInstances
            .Include(instance => instance.Subscription)
            .SingleAsync(instance => instance.Id == CustomerInstanceId, cancellationToken);
}
