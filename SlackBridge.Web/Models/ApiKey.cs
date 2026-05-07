using System.ComponentModel.DataAnnotations;

namespace SlackBridge.Web.Models;

public sealed class ApiKey
{
    public int Id { get; set; }

    public int CustomerInstanceId { get; set; }
    public CustomerInstance? CustomerInstance { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string KeyHash { get; set; } = string.Empty;

    [MaxLength(12)]
    public string KeyPrefix { get; set; } = string.Empty;

    public string? EncryptedKey { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedAtUtc { get; set; }
}
