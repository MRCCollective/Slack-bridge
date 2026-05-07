using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;

namespace SlackBridge.Web.Pages.Admin.EventDefinitions;

public sealed class DeleteModel(SlackBridgeDbContext dbContext) : PageModel
{
    [BindProperty]
    public EventDefinition EventDefinition { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var definition = await dbContext.EventDefinitions.FindAsync([id], cancellationToken);
        if (definition is null)
        {
            return NotFound();
        }

        EventDefinition = definition;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var definition = await dbContext.EventDefinitions.FindAsync([EventDefinition.Id], cancellationToken);
        if (definition is not null)
        {
            dbContext.EventDefinitions.Remove(definition);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return RedirectToPage("Index");
    }
}
