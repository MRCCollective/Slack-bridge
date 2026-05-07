using SlackBridge.Web.Models;

namespace SlackBridge.Web.Services;

public sealed record PlanLimitSet(int EventsPerMonth, int ApiKeys, int Projects);

public interface IPlanLimitService
{
    PlanLimitSet GetLimits(PlanType plan);
}

public sealed class PlanLimitService : IPlanLimitService
{
    public PlanLimitSet GetLimits(PlanType plan) =>
        plan switch
        {
            PlanType.Free => new PlanLimitSet(500, 2, 1),
            PlanType.Pro => new PlanLimitSet(25_000, 20, 25),
            PlanType.Scale => new PlanLimitSet(250_000, 100, 250),
            _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, null)
        };
}
