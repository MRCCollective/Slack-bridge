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

    [Required]
    public string Template { get; set; } = string.Empty;

    public bool UseCustomSlackWebhook { get; set; }

    [Url, MaxLength(2048)]
    public string? CustomSlackWebhookUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<EventLog> EventLogs { get; set; } = [];

    public string GetSlackWebhookUrl()
    {
        if (UseCustomSlackWebhook && !string.IsNullOrWhiteSpace(CustomSlackWebhookUrl))
        {
            return CustomSlackWebhookUrl;
        }

        return Project?.SlackWebhookUrl ?? string.Empty;
    }
}
