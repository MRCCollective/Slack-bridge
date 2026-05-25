using System.ComponentModel.DataAnnotations;

namespace SlackBridge.Web.Models;

public sealed class Project
{
    public int Id { get; set; }

    public int CustomerInstanceId { get; set; }
    public CustomerInstance? CustomerInstance { get; set; }

    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required, Url, MaxLength(2048)]
    public string SlackWebhookUrl { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ApiKey> ApiKeys { get; set; } = [];
    public ICollection<EventDefinition> EventDefinitions { get; set; } = [];
}
