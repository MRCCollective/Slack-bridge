namespace SlackBridge.Web.Models;

public sealed class UsageMetric
{
    public int Id { get; set; }

    public int CustomerInstanceId { get; set; }
    public CustomerInstance? CustomerInstance { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }
    public int EventsSent { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
