using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.ApiKeys;

public sealed class DeleteModel(
    SlackBridgeDbContext dbContext,
    ICustomerInstanceContext customerInstanceContext) : PageModel
{
    [BindProperty]
    public ApiKey ApiKey { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var apiKey = await dbContext.ApiKeys.SingleOrDefaultAsync(
            apiKey => apiKey.Id == id && apiKey.CustomerInstanceId == customerInstanceContext.CustomerInstanceId,
            cancellationToken);
        if (apiKey is null)
        {
            return NotFound();
        }

        ApiKey = apiKey;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var apiKey = await dbContext.ApiKeys.SingleOrDefaultAsync(
            apiKey => apiKey.Id == ApiKey.Id && apiKey.CustomerInstanceId == customerInstanceContext.CustomerInstanceId,
            cancellationToken);
        var projectId = apiKey?.ProjectId;
        if (apiKey is not null)
        {
            dbContext.ApiKeys.Remove(apiKey);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return projectId.HasValue
            ? RedirectToPage("/Admin/Projects/Details", new { id = projectId.Value })
            : RedirectToPage("/Admin/Projects/Index");
    }
}
