using System.ComponentModel.DataAnnotations;

namespace SlackBridge.Web.Models;

public sealed class SlackCommandRoute
{
    public int Id { get; set; }

    public int CustomerInstanceId { get; set; }
    public CustomerInstance? CustomerInstance { get; set; }

    [Display(Name = "Slack bot")]
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public bool IsActive { get; set; } = true;

    [Display(Name = "Slash command")]
    [Required, MaxLength(80)]
    [RegularExpression("^/[A-Za-z0-9_-]+$", ErrorMessage = "Use a Slack slash command such as /shoppingtajm.")]
    public string SlackCommand { get; set; } = string.Empty;

    [Required]
    public string EncryptedSlackSigningSecret { get; set; } = string.Empty;

    [Display(Name = "Downstream URL")]
    [Required, Url, MaxLength(2048)]
    public string DownstreamUrl { get; set; } = string.Empty;

    [Display(Name = "Downstream auth header")]
    [MaxLength(120)]
    [RegularExpression("^[A-Za-z0-9][A-Za-z0-9_-]*$", ErrorMessage = "Use a simple HTTP header name such as x-slackbridge-secret.")]
    public string DownstreamAuthHeaderName { get; set; } = "x-slackbridge-secret";

    public string? EncryptedDownstreamAuthSecret { get; set; }

    [Display(Name = "Allowed Slack team ID")]
    [MaxLength(80)]
    public string? AllowedTeamId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
