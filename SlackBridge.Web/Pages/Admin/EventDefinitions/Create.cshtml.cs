using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.EventDefinitions;

public sealed class CreateModel(
    SlackBridgeDbContext dbContext,
    ICustomerInstanceContext customerInstanceContext,
    IEventDefinitionTestService eventDefinitionTestService) : PageModel
{
    [BindProperty]
    public EventDefinition EventDefinition { get; set; } = new()
    {
        Template = "New event: {{ message }}",
        IsActive = true
    };

    public SelectList ProjectOptions { get; private set; } = new(Array.Empty<object>());
    public string? StatusMessage { get; private set; }
    public bool StatusSucceeded { get; private set; }

    [BindProperty]
    public string SubmitAction { get; set; } = "Create";

    public async Task OnGetAsync(int? projectId, CancellationToken cancellationToken)
    {
        if (projectId.HasValue)
        {
            EventDefinition.ProjectId = projectId.Value;
        }

        await LoadProjectsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        ValidateWebhookOverride();

        if (string.Equals(SubmitAction, "Test", StringComparison.OrdinalIgnoreCase))
        {
            return await SendTestAsync(cancellationToken);
        }

        if (!ModelState.IsValid)
        {
            await LoadProjectsAsync(cancellationToken);
            return Page();
        }

        EventDefinition.CustomerInstanceId = customerInstanceContext.CustomerInstanceId;
        dbContext.EventDefinitions.Add(EventDefinition);
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToPage("/Admin/Projects/Details", new { id = EventDefinition.ProjectId });
    }

    private async Task<IActionResult> SendTestAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadProjectsAsync(cancellationToken);
            return Page();
        }

        EventDefinition.CustomerInstanceId = customerInstanceContext.CustomerInstanceId;
        EventDefinitionTestResult result;
        try
        {
            result = await eventDefinitionTestService.SendAsync(EventDefinition, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadProjectsAsync(cancellationToken);
            return Page();
        }

        StatusMessage = $"{result.Message} Log #{result.LogId}.";
        StatusSucceeded = result.Succeeded;

        await LoadProjectsAsync(cancellationToken);
        return Page();
    }

    private void ValidateWebhookOverride()
    {
        if (EventDefinition.UseCustomSlackWebhook &&
            string.IsNullOrWhiteSpace(EventDefinition.CustomSlackWebhookUrl))
        {
            ModelState.AddModelError(
                "EventDefinition.CustomSlackWebhookUrl",
                "Enter a custom Slack webhook URL or turn off the event-level override.");
        }
    }

    private async Task LoadProjectsAsync(CancellationToken cancellationToken)
    {
        var projects = await dbContext.Projects
            .Where(project => project.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);
        ProjectOptions = new SelectList(projects, "Id", "Name", EventDefinition.ProjectId);
    }
}
