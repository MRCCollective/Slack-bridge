using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;

namespace SlackBridge.Web.Services;

public interface IEventDefinitionTestService
{
    Task<EventDefinitionTestResult> SendAsync(EventDefinition definition, CancellationToken cancellationToken);
}

public sealed record EventDefinitionTestResult(bool Succeeded, long LogId, string Message);

public sealed class EventDefinitionTestService(
    SlackBridgeDbContext dbContext,
    ITemplateService templateService,
    ISlackService slackService,
    IEventLogService eventLogService,
    ICustomerInstanceContext customerInstanceContext) : IEventDefinitionTestService
{
    public async Task<EventDefinitionTestResult> SendAsync(EventDefinition definition, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .SingleOrDefaultAsync(project =>
            project.Id == definition.ProjectId &&
            project.CustomerInstanceId == customerInstanceContext.CustomerInstanceId,
            cancellationToken);

        if (project is null)
        {
            throw new InvalidOperationException("Choose a project before sending a test.");
        }

        definition.Project = project;
        var webhookUrl = definition.GetSlackWebhookUrl();

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            throw new InvalidOperationException("Add a Slack webhook URL to the project, or enable a custom webhook URL for this event.");
        }

        var payloadJson = JsonSerializer.Serialize(new
        {
            message = "Slack Bridge test alert",
            event_key = definition.Key,
            project_id = definition.ProjectId,
            sent_at_utc = DateTimeOffset.UtcNow
        });

        string? renderedMessage = null;

        try
        {
            using var payload = JsonDocument.Parse(payloadJson);
            renderedMessage = await templateService.RenderAsync(definition.Template, payload.RootElement, cancellationToken);
            var slackResult = await slackService.SendAsync(webhookUrl, renderedMessage, cancellationToken);

            var log = await eventLogService.WriteAsync(new EventLog
            {
                CustomerInstanceId = customerInstanceContext.CustomerInstanceId,
                ProjectId = definition.ProjectId,
                EventDefinitionId = definition.Id == 0 ? null : definition.Id,
                EventKey = definition.Key,
                PayloadJson = payloadJson,
                RenderedMessage = renderedMessage,
                Status = EventLogStatus.Succeeded,
                ResultMessage = "Test alert sent.",
                SlackStatusCode = (int)slackResult.StatusCode
            }, cancellationToken);

            return new EventDefinitionTestResult(true, log.Id, "Test alert sent to Slack.");
        }
        catch (Exception exception)
        {
            var log = await eventLogService.WriteAsync(new EventLog
            {
                CustomerInstanceId = customerInstanceContext.CustomerInstanceId,
                ProjectId = definition.ProjectId,
                EventDefinitionId = definition.Id == 0 ? null : definition.Id,
                EventKey = string.IsNullOrWhiteSpace(definition.Key) ? "test" : definition.Key,
                PayloadJson = payloadJson,
                RenderedMessage = renderedMessage,
                Status = EventLogStatus.Failed,
                ResultMessage = exception.Message,
                SlackStatusCode = exception is SlackDeliveryException slackException ? (int)slackException.StatusCode : null
            }, cancellationToken);

            return new EventDefinitionTestResult(false, log.Id, $"Test failed: {exception.Message}");
        }
    }
}
