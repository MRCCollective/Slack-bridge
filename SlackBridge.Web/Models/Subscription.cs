using System.ComponentModel.DataAnnotations;

namespace SlackBridge.Web.Models;

public sealed class Subscription
{
    public int Id { get; set; }

    public int CustomerInstanceId { get; set; }
    public CustomerInstance? CustomerInstance { get; set; }

    public PlanType Plan { get; set; } = PlanType.Free;

    [MaxLength(80)]
    public string Status { get; set; } = "active";

    public DateTimeOffset? CurrentPeriodEndUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
