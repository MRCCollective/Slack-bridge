using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.SlackCommandRoutes;

public sealed class IndexModel(
    SlackBridgeDbContext dbContext,
    ICustomerInstanceContext customerInstanceContext) : PageModel
{
    public IReadOnlyList<RouteRow> Routes { get; private set; } = [];
    public string RequestUrl => $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/slack/commands";

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, CancellationToken cancellationToken)
    {
        var route = await dbContext.SlackCommandRoutes
            .SingleOrDefaultAsync(route =>
                route.Id == id &&
                route.CustomerInstanceId == customerInstanceContext.CustomerInstanceId,
                cancellationToken);

        if (route is null)
        {
            return NotFound();
        }

        route.IsActive = !route.IsActive;
        route.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Routes = await dbContext.SlackCommandRoutes
            .Include(route => route.Project)
            .Where(route => route.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderBy(route => route.SlackCommand)
            .ThenBy(route => route.Project!.Name)
            .Select(route => new RouteRow(
                route.Id,
                route.Project!.Name,
                route.SlackCommand,
                route.DownstreamUrl,
                route.AllowedTeamId,
                route.IsActive))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public sealed record RouteRow(
        int Id,
        string ProjectName,
        string SlackCommand,
        string DownstreamUrl,
        string? AllowedTeamId,
        bool IsActive);
}
