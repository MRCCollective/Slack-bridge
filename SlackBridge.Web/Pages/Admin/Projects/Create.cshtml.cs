using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.Projects;

public sealed class CreateModel(
    SlackBridgeDbContext dbContext,
    ICustomerInstanceContext customerInstanceContext,
    IUsageService usageService) : PageModel
{
    [BindProperty]
    public Project Project { get; set; } = new();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await usageService.EnsureProjectLimitAsync(cancellationToken);
        }
        catch (PlanLimitExceededException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }

        Project.CustomerInstanceId = customerInstanceContext.CustomerInstanceId;
        dbContext.Projects.Add(Project);
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToPage("Index");
    }
}
