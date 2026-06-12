using System.ComponentModel.DataAnnotations;

namespace SlackBridge.Web.Models;

public sealed class SlackCommandLog
{
    public long Id { get; set; }

    public int CustomerInstanceId { get; set; }
    public CustomerInstance? CustomerInstance { get; set; }

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? SlackCommandRouteId { get; set; }
    public SlackCommandRoute? SlackCommandRoute { get; set; }

    [MaxLength(80)]
    public string? Command { get; set; }

    [MaxLength(80)]
    public string? TeamId { get; set; }

    [MaxLength(80)]
    public string? ChannelId { get; set; }

    [MaxLength(80)]
    public string? UserId { get; set; }

    public int? DownstreamStatusCode { get; set; }
    public SlackCommandLogStatus Status { get; set; }

    [MaxLength(1000)]
    public string? ResultMessage { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
