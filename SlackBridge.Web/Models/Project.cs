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

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ApiKey> ApiKeys { get; set; } = [];
    public ICollection<EventDefinition> EventDefinitions { get; set; } = [];
}
