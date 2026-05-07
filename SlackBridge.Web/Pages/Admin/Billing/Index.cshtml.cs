using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.Billing;

[Authorize(Roles = ApplicationRoles.Admin)]
public sealed class IndexModel(IBillingService billingService, IPlanLimitService planLimitService) : PageModel
{
    public Subscription Subscription { get; private set; } = new();
    public IReadOnlyList<PlanView> Plans { get; private set; } = [];
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(string? statusMessage, CancellationToken cancellationToken)
    {
        StatusMessage = statusMessage;
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(PlanType plan, CancellationToken cancellationToken)
    {
        await billingService.ChangePlanAsync(plan, cancellationToken);
        return RedirectToPage("Index", new { statusMessage = $"Plan changed to {plan}." });
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Subscription = await billingService.GetSubscriptionAsync(cancellationToken);
        Plans = Enum.GetValues<PlanType>()
            .Select(plan => new PlanView(plan, planLimitService.GetLimits(plan)))
            .ToList();
    }

    public sealed record PlanView(PlanType Plan, PlanLimitSet Limits);
}
