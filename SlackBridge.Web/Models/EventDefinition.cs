using System.ComponentModel.DataAnnotations;

namespace SlackBridge.Web.Models;

public sealed class EventDefinition
{
    public int Id { get; set; }

    public int CustomerInstanceId { get; set; }
    public CustomerInstance? CustomerInstance { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    [Required, MaxLength(120)]
    public string Key { get; set; } = string.Empty;

    [Required, Url, MaxLength(2048)]
    public string SlackWebhookUrl { get; set; } = string.Empty;

    [Required]
    public string Template { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<EventLog> EventLogs { get; set; } = [];
}
