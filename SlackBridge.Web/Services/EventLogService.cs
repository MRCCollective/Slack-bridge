using System.Text.Json;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;

namespace SlackBridge.Web.Services;

public interface IEventLogService
{
    Task<EventLog> WriteAsync(EventLog log, CancellationToken cancellationToken);
}

public sealed class EventLogService(SlackBridgeDbContext dbContext) : IEventLogService
{
    public async Task<EventLog> WriteAsync(EventLog log, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(log.PayloadJson))
        {
            log.PayloadJson = JsonSerializer.Serialize(new { });
        }

        dbContext.EventLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);
        return log;
    }
}
