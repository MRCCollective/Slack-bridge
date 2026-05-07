using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Account;

public sealed class RegisterModel(
    SlackBridgeDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ICustomerInstanceContext customerInstanceContext) : PageModel
{
    [BindProperty]
    public RegisterInput Input { get; set; } = new();

    public bool SetupComplete { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        SetupComplete = await dbContext.Users.AnyAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        SetupComplete = await dbContext.Users.AnyAsync(cancellationToken);
        if (SetupComplete)
        {
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var instance = await customerInstanceContext.GetAsync(cancellationToken);
        instance.CompanyName = Input.CompanyName;

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            EmailConfirmed = true,
            CustomerInstanceId = customerInstanceContext.CustomerInstanceId
        };

        var result = await userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        await userManager.AddToRoleAsync(user, ApplicationRoles.Admin);
        await dbContext.SaveChangesAsync(cancellationToken);
        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToPage("/Admin/Usage/Index");
    }

    public sealed class RegisterInput
    {
        [Required, MaxLength(180), Display(Name = "Company name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
