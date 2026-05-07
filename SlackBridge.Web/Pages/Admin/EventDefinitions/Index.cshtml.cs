using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.EventDefinitions;

public sealed class IndexModel(
    SlackBridgeDbContext dbContext,
    ITemplateService templateService,
    ISlackService slackService,
    IEventLogService eventLogService,
    ICustomerInstanceContext customerInstanceContext) : PageModel
{
    public IReadOnlyList<EventDefinition> EventDefinitions { get; private set; } = [];
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(string? statusMessage, CancellationToken cancellationToken)
    {
        StatusMessage = statusMessage;
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostTestAsync(int id, CancellationToken cancellationToken)
    {
        var definition = await dbContext.EventDefinitions
            .Include(eventDefinition => eventDefinition.Project)
            .SingleOrDefaultAsync(eventDefinition => eventDefinition.Id == id, cancellationToken);

        if (definition is null)
        {
            return NotFound();
        }

        using var document = JsonDocument.Parse("""
        {
          "test": true,
          "message": "Slack Bridge test",
          "sent_at_utc": "2026-05-07T00:00:00Z"
        }
        """);

        string? rendered = null;
        try
        {
            rendered = await templateService.RenderAsync(definition.Template, document.RootElement, cancellationToken);
            var result = await slackService.SendAsync(definition.SlackWebhookUrl, rendered, cancellationToken);
            await eventLogService.WriteAsync(new EventLog
            {
                CustomerInstanceId = customerInstanceContext.CustomerInstanceId,
                ProjectId = definition.ProjectId,
                EventDefinitionId = definition.Id,
                EventKey = definition.Key,
                PayloadJson = document.RootElement.GetRawText(),
                RenderedMessage = rendered,
                Status = EventLogStatus.Succeeded,
                ResultMessage = result.ResponseBody,
                SlackStatusCode = (int)result.StatusCode
            }, cancellationToken);

            return RedirectToPage("Index", new { statusMessage = $"Test send for '{definition.Key}' succeeded." });
        }
        catch (Exception exception)
        {
            await eventLogService.WriteAsync(new EventLog
            {
                CustomerInstanceId = customerInstanceContext.CustomerInstanceId,
                ProjectId = definition.ProjectId,
                EventDefinitionId = definition.Id,
                EventKey = definition.Key,
                PayloadJson = document.RootElement.GetRawText(),
                RenderedMessage = rendered,
                Status = EventLogStatus.Failed,
                ResultMessage = exception.Message,
                SlackStatusCode = exception is SlackDeliveryException slackException ? (int)slackException.StatusCode : null
            }, cancellationToken);

            return RedirectToPage("Index", new { statusMessage = $"Test send for '{definition.Key}' failed: {exception.Message}" });
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        EventDefinitions = await dbContext.EventDefinitions
            .Include(eventDefinition => eventDefinition.Project)
            .Where(eventDefinition => eventDefinition.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderBy(eventDefinition => eventDefinition.Project!.Name)
            .ThenBy(eventDefinition => eventDefinition.Key)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
