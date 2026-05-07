using System.ComponentModel.DataAnnotations;

namespace SlackBridge.Web.Models;

public sealed class CustomerInstance
{
    public int Id { get; set; }

    [Required, MaxLength(180)]
    public string CompanyName { get; set; } = "Workspace";

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Subscription? Subscription { get; set; }
    public ICollection<ApplicationUser> Users { get; set; } = [];
}
