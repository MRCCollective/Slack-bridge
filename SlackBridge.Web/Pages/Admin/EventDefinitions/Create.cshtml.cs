using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.EventDefinitions;

public sealed class CreateModel(SlackBridgeDbContext dbContext, ICustomerInstanceContext customerInstanceContext) : PageModel
{
    [BindProperty]
    public EventDefinition EventDefinition { get; set; } = new()
    {
        Template = "New event: {{ message }}",
        IsActive = true
    };

    public SelectList ProjectOptions { get; private set; } = new(Array.Empty<object>());

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadProjectsAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadProjectsAsync(cancellationToken);
            return Page();
        }

        EventDefinition.CustomerInstanceId = customerInstanceContext.CustomerInstanceId;
        dbContext.EventDefinitions.Add(EventDefinition);
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToPage("Index");
    }

    private async Task LoadProjectsAsync(CancellationToken cancellationToken)
    {
        var projects = await dbContext.Projects
            .Where(project => project.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);
        ProjectOptions = new SelectList(projects, "Id", "Name");
    }
}
