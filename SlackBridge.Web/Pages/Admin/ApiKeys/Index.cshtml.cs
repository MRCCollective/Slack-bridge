using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.ApiKeys;

public sealed class IndexModel(SlackBridgeDbContext dbContext, ICustomerInstanceContext customerInstanceContext) : PageModel
{
    public IReadOnlyList<ApiKey> ApiKeys { get; private set; } = [];
    public string? CreatedKey { get; private set; }

    public async Task OnGetAsync(string? createdKey, CancellationToken cancellationToken)
    {
        CreatedKey = createdKey;
        ApiKeys = await dbContext.ApiKeys
            .Include(apiKey => apiKey.Project)
            .Where(apiKey => apiKey.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderBy(apiKey => apiKey.Project!.Name)
            .ThenBy(apiKey => apiKey.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
