using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.Logs;

public sealed class IndexModel(
    SlackBridgeDbContext dbContext,
    ICustomerInstanceContext customerInstanceContext,
    ILocalClock localClock) : PageModel
{
    public IReadOnlyList<LogRow> Logs { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var logs = await dbContext.EventLogs
            .Where(log => log.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderByDescending(log => log.CreatedAtUtc)
            .Take(200)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Logs = logs
            .Select(log => new LogRow(
                localClock.Format(log.CreatedAtUtc),
                log.EventKey,
                log.Status.ToString(),
                log.SlackStatusCode,
                log.ResultMessage))
            .ToList();
    }

    public sealed record LogRow(
        string Created,
        string EventKey,
        string Status,
        int? SlackStatusCode,
        string? ResultMessage);
}
