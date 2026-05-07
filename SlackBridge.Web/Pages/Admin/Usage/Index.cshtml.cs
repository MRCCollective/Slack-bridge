using Microsoft.AspNetCore.Mvc.RazorPages;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.Usage;

public sealed class IndexModel(IUsageService usageService) : PageModel
{
    public UsageSnapshot Snapshot { get; private set; } =
        new(Models.PlanType.Free, new PlanLimitSet(0, 0, 0), 0, 0, 0);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Snapshot = await usageService.GetCurrentAsync(cancellationToken);
    }
}
