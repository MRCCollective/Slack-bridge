using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.EventDefinitions;

public sealed class DeleteModel(
    SlackBridgeDbContext dbContext,
    ICustomerInstanceContext customerInstanceContext) : PageModel
{
    [BindProperty]
    public EventDefinition EventDefinition { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var definition = await dbContext.EventDefinitions.SingleOrDefaultAsync(
            definition => definition.Id == id && definition.CustomerInstanceId == customerInstanceContext.CustomerInstanceId,
            cancellationToken);
        if (definition is null)
        {
            return NotFound();
        }

        EventDefinition = definition;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var definition = await dbContext.EventDefinitions.SingleOrDefaultAsync(
            definition => definition.Id == EventDefinition.Id &&
                definition.CustomerInstanceId == customerInstanceContext.CustomerInstanceId,
            cancellationToken);
        var projectId = definition?.ProjectId;
        if (definition is not null)
        {
            dbContext.EventDefinitions.Remove(definition);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return projectId.HasValue
            ? RedirectToPage("/Admin/Projects/Details", new { id = projectId.Value })
            : RedirectToPage("/Admin/Projects/Index");
    }
}
