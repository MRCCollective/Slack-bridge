using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;

namespace SlackBridge.Web.Pages.Admin.Projects;

public sealed class DeleteModel(SlackBridgeDbContext dbContext) : PageModel
{
    [BindProperty]
    public Project Project { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FindAsync([id], cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        Project = project;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FindAsync([Project.Id], cancellationToken);
        if (project is not null)
        {
            dbContext.Projects.Remove(project);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return RedirectToPage("Index");
    }
}
