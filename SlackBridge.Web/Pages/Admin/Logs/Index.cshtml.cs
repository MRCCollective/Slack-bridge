using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.Logs;

public sealed class IndexModel(SlackBridgeDbContext dbContext, ICustomerInstanceContext customerInstanceContext) : PageModel
{
    public IReadOnlyList<EventLog> Logs { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Logs = await dbContext.EventLogs
            .Where(log => log.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderByDescending(log => log.CreatedAtUtc)
            .Take(200)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
