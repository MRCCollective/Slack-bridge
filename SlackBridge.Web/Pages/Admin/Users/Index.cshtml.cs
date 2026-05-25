using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.Users;

[Authorize(Roles = ApplicationRoles.SuperUser)]
public sealed class IndexModel(
    SlackBridgeDbContext dbContext,
    UserManager<ApplicationUser> userManager) : PageModel
{
    public IReadOnlyList<UserRow> Users { get; private set; } = [];
    public IReadOnlyList<TenantRow> Tenants { get; private set; } = [];
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(string? statusMessage, CancellationToken cancellationToken)
    {
        StatusMessage = statusMessage;
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostTenantAsync(
        string id,
        int customerInstanceId,
        CancellationToken cancellationToken)
    {
        var currentUserId = userManager.GetUserId(User);
        if (id == currentUserId)
        {
            return RedirectToPage(new { statusMessage = "You cannot move your own superuser account." });
        }

        var tenantExists = await dbContext.CustomerInstances
            .AnyAsync(instance => instance.Id == customerInstanceId, cancellationToken);
        if (!tenantExists)
        {
            return RedirectToPage(new { statusMessage = "Tenant was not found." });
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return RedirectToPage(new { statusMessage = "User was already deleted." });
        }

        if (await userManager.IsInRoleAsync(user, ApplicationRoles.SuperUser))
        {
            return RedirectToPage(new { statusMessage = "Superuser accounts cannot be moved here." });
        }

        user.CustomerInstanceId = customerInstanceId;
        var result = await userManager.UpdateAsync(user);
        var message = result.Succeeded
            ? $"Moved {user.Email} to tenant {customerInstanceId}. They should sign in again to refresh access."
            : string.Join(" ", result.Errors.Select(error => error.Description));

        return RedirectToPage(new { statusMessage = message });
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id, CancellationToken cancellationToken)
    {
        var currentUserId = userManager.GetUserId(User);
        if (id == currentUserId)
        {
            return RedirectToPage(new { statusMessage = "You cannot delete your own superuser account." });
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return RedirectToPage(new { statusMessage = "User was already deleted." });
        }

        if (await userManager.IsInRoleAsync(user, ApplicationRoles.SuperUser))
        {
            return RedirectToPage(new { statusMessage = "Superuser accounts cannot be deleted here." });
        }

        var result = await userManager.DeleteAsync(user);
        var message = result.Succeeded
            ? $"Deleted {user.Email}."
            : string.Join(" ", result.Errors.Select(error => error.Description));

        return RedirectToPage(new { statusMessage = message });
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var currentUserId = userManager.GetUserId(User);
        Tenants = await dbContext.CustomerInstances
            .OrderBy(instance => instance.CompanyName)
            .Select(instance => new TenantRow(instance.Id, instance.CompanyName))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var users = await dbContext.Users
            .Include(user => user.CustomerInstance)
            .OrderBy(user => user.Email)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var rows = new List<UserRow>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            rows.Add(new UserRow(
                user.Id,
                user.Email ?? user.UserName ?? "",
                user.CustomerInstanceId,
                user.CustomerInstance?.CompanyName ?? "",
                string.Join(", ", roles.OrderBy(role => role)),
                user.Id == currentUserId,
                roles.Contains(ApplicationRoles.SuperUser)));
        }

        Users = rows;
    }

    public sealed record UserRow(
        string Id,
        string Email,
        int CustomerInstanceId,
        string CompanyName,
        string Roles,
        bool IsCurrentUser,
        bool IsSuperUser);

    public sealed record TenantRow(int Id, string CompanyName);
}
