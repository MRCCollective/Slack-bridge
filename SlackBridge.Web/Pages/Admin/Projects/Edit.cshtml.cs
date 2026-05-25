using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.Projects;

public sealed class EditModel(
    SlackBridgeDbContext dbContext,
    ICustomerInstanceContext customerInstanceContext) : PageModel
{
    [BindProperty]
    public Project Project { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.SingleOrDefaultAsync(
            project => project.Id == id && project.CustomerInstanceId == customerInstanceContext.CustomerInstanceId,
            cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        Project = project;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var project = await dbContext.Projects.SingleOrDefaultAsync(
            project => project.Id == Project.Id && project.CustomerInstanceId == customerInstanceContext.CustomerInstanceId,
            cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        project.Name = Project.Name;
        project.Description = Project.Description;
        project.SlackWebhookUrl = Project.SlackWebhookUrl;
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToPage("Details", new { id = Project.Id });
    }
}
