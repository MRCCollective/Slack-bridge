using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;

namespace SlackBridge.Web.Pages.Admin.ApiKeys;

public sealed class DeleteModel(SlackBridgeDbContext dbContext) : PageModel
{
    [BindProperty]
    public ApiKey ApiKey { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var apiKey = await dbContext.ApiKeys.FindAsync([id], cancellationToken);
        if (apiKey is null)
        {
            return NotFound();
        }

        ApiKey = apiKey;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var apiKey = await dbContext.ApiKeys.FindAsync([ApiKey.Id], cancellationToken);
        if (apiKey is not null)
        {
            dbContext.ApiKeys.Remove(apiKey);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return RedirectToPage("Index");
    }
}
