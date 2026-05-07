using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.ApiKeys;

public sealed class EditModel(SlackBridgeDbContext dbContext, ICustomerInstanceContext customerInstanceContext) : PageModel
{
    [BindProperty]
    public ApiKey ApiKey { get; set; } = new();

    public SelectList ProjectOptions { get; private set; } = new(Array.Empty<object>());

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var apiKey = await dbContext.ApiKeys.FindAsync([id], cancellationToken);
        if (apiKey is null)
        {
            return NotFound();
        }

        ApiKey = apiKey;
        await LoadProjectsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadProjectsAsync(cancellationToken);
            return Page();
        }

        dbContext.Attach(ApiKey).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToPage("Index");
    }

    private async Task LoadProjectsAsync(CancellationToken cancellationToken)
    {
        var projects = await dbContext.Projects
            .Where(project => project.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);
        ProjectOptions = new SelectList(projects, "Id", "Name", ApiKey.ProjectId);
    }
}
