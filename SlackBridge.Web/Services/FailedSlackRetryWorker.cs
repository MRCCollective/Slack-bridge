using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;

namespace SlackBridge.Web.Services;

public sealed class FailedSlackRetryWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<FailedSlackRetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RetryBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed Slack retry batch crashed.");
            }
        }
    }

    private async Task RetryBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SlackBridgeDbContext>();
        var slackService = scope.ServiceProvider.GetRequiredService<ISlackService>();
        var now = DateTimeOffset.UtcNow;

        var logs = await dbContext.EventLogs
            .Include(log => log.EventDefinition)
            .ThenInclude(definition => definition!.Project)
            .Include(log => log.Project)
            .Where(log =>
                log.Status == EventLogStatus.Failed &&
                log.RetryState == RetryState.Pending &&
                log.RenderedMessage != null &&
                log.EventDefinition != null &&
                log.Project != null &&
                log.NextRetryAtUtc <= now)
            .OrderBy(log => log.CreatedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var log in logs)
        {
            try
            {
                var result = await slackService.SendAsync(log.EventDefinition!.GetSlackWebhookUrl(), log.RenderedMessage!, cancellationToken);
                log.Status = EventLogStatus.Succeeded;
                log.RetryState = RetryState.None;
                log.ResultMessage = $"Retried successfully: {result.ResponseBody}";
                log.SlackStatusCode = (int)result.StatusCode;
            }
            catch (Exception exception)
            {
                log.RetryCount++;
                log.ResultMessage = exception.Message;
                log.RetryState = log.RetryCount >= 3 ? RetryState.Exhausted : RetryState.Pending;
                log.NextRetryAtUtc = log.RetryState == RetryState.Pending
                    ? DateTimeOffset.UtcNow.AddMinutes(Math.Pow(2, log.RetryCount))
                    : null;
            }
        }

        if (logs.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
