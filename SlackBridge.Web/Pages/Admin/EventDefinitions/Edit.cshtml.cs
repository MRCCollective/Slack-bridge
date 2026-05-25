using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.EventDefinitions;

public sealed class EditModel(SlackBridgeDbContext dbContext, ICustomerInstanceContext customerInstanceContext) : PageModel
{
    [BindProperty]
    public EventDefinition EventDefinition { get; set; } = new();

    public SelectList ProjectOptions { get; private set; } = new(Array.Empty<object>());

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var definition = await dbContext.EventDefinitions.FindAsync([id], cancellationToken);
        if (definition is null)
        {
            return NotFound();
        }

        EventDefinition = definition;
        await LoadProjectsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        ValidateWebhookOverride();

        if (!ModelState.IsValid)
        {
            await LoadProjectsAsync(cancellationToken);
            return Page();
        }

        dbContext.Attach(EventDefinition).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToPage("/Admin/Projects/Details", new { id = EventDefinition.ProjectId });
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
