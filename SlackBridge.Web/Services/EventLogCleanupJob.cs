using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;

namespace SlackBridge.Web.Services;

public sealed class EventLogCleanupJob(
    SlackBridgeDbContext dbContext,
    ILogger<EventLogCleanupJob> logger)
{
    public async Task DeleteOldLogsAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMonths(-3);
        var deleted = await dbContext.EventLogs
            .Where(log => log.CreatedAtUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation(
            "Event log cleanup deleted {DeletedCount} rows older than {CutoffUtc}.",
            deleted,
            cutoff);
    }
}
