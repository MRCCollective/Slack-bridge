namespace SlackBridge.Web.Models;

public sealed class EventLog
{
    public long Id { get; set; }

    public int CustomerInstanceId { get; set; }
    public CustomerInstance? CustomerInstance { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? EventDefinitionId { get; set; }
    public EventDefinition? EventDefinition { get; set; }

    public string EventKey { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string? RenderedMessage { get; set; }
    public EventLogStatus Status { get; set; }
    public string? ResultMessage { get; set; }
    public int? SlackStatusCode { get; set; }
    public RetryState RetryState { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset? NextRetryAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
