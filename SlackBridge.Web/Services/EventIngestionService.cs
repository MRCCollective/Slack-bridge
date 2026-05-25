using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Contracts;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;

namespace SlackBridge.Web.Services;

public interface IEventIngestionService
{
    Task<EventResponse> HandleAsync(ApiKey apiKey, EventRequest request, CancellationToken cancellationToken);
}

public sealed class EventIngestionService(
    SlackBridgeDbContext dbContext,
    ITemplateService templateService,
    ISlackService slackService,
    IEventLogService eventLogService,
    IUsageService usageService) : IEventIngestionService
{
    public async Task<EventResponse> HandleAsync(ApiKey apiKey, EventRequest request, CancellationToken cancellationToken)
    {
        await usageService.EnsureEventLimitAsync(apiKey.CustomerInstanceId, cancellationToken);

        var definition = await dbContext.EventDefinitions
            .Include(definition => definition.Project)
            .SingleOrDefaultAsync(definition =>
                definition.CustomerInstanceId == apiKey.CustomerInstanceId &&
                definition.ProjectId == apiKey.ProjectId &&
                definition.Key == request.Key &&
                definition.IsActive,
                cancellationToken);

        if (definition is null)
        {
            var missingLog = await eventLogService.WriteAsync(new EventLog
            {
                CustomerInstanceId = apiKey.CustomerInstanceId,
                ProjectId = apiKey.ProjectId,
                EventKey = request.Key,
                PayloadJson = request.Data.GetRawText(),
                Status = EventLogStatus.Failed,
                ResultMessage = "No active event definition matched this key."
            }, cancellationToken);

            throw new EventDefinitionNotFoundException(missingLog.Id, request.Key);
        }

        string? renderedMessage = null;

        try
        {
            renderedMessage = await templateService.RenderAsync(definition.Template, request.Data, cancellationToken);
            var slackResult = await slackService.SendAsync(definition.GetSlackWebhookUrl(), renderedMessage, cancellationToken);

            var successLog = await eventLogService.WriteAsync(new EventLog
            {
                CustomerInstanceId = apiKey.CustomerInstanceId,
                ProjectId = apiKey.ProjectId,
                EventDefinitionId = definition.Id,
                EventKey = request.Key,
                PayloadJson = request.Data.GetRawText(),
                RenderedMessage = renderedMessage,
                Status = EventLogStatus.Succeeded,
                ResultMessage = slackResult.ResponseBody,
                SlackStatusCode = (int)slackResult.StatusCode
            }, cancellationToken);

            await usageService.IncrementEventsAsync(cancellationToken);
            return new EventResponse(successLog.Id, "succeeded");
        }
        catch (Exception exception)
        {
            var failedLog = await eventLogService.WriteAsync(new EventLog
            {
                CustomerInstanceId = apiKey.CustomerInstanceId,
                ProjectId = apiKey.ProjectId,
                EventDefinitionId = definition.Id,
                EventKey = request.Key,
                PayloadJson = request.Data.GetRawText(),
                RenderedMessage = renderedMessage,
                Status = EventLogStatus.Failed,
                ResultMessage = exception.Message,
                SlackStatusCode = exception is SlackDeliveryException slackException ? (int)slackException.StatusCode : null,
                RetryState = renderedMessage is null ? RetryState.None : RetryState.Pending,
                NextRetryAtUtc = renderedMessage is null ? null : DateTimeOffset.UtcNow.AddMinutes(1)
            }, cancellationToken);

            throw new EventDeliveryException(failedLog.Id, exception);
        }
    }
}

public sealed class EventDefinitionNotFoundException(long logId, string eventKey)
    : Exception($"No active event definition found for '{eventKey}'.")
{
    public long LogId { get; } = logId;
}

public sealed class EventDeliveryException(long logId, Exception innerException)
    : Exception("Event delivery failed.", innerException)
{
    public long LogId { get; } = logId;
}
