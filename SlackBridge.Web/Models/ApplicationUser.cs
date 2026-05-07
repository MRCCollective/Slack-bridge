using Microsoft.AspNetCore.Identity;

namespace SlackBridge.Web.Models;

public sealed class ApplicationUser : IdentityUser
{
    public int CustomerInstanceId { get; set; }
    public CustomerInstance? CustomerInstance { get; set; }
}
