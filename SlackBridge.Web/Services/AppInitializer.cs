using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;

namespace SlackBridge.Web.Services;

public static class AppInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SlackBridgeDbContext>();

        if (configuration.GetValue<bool>("Database:EnsureCreated"))
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in new[] { ApplicationRoles.SuperUser, ApplicationRoles.Admin, ApplicationRoles.Member })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (environment.IsProduction())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var oldSuperUser = await userManager.FindByEmailAsync("daniel@mrccollective");
            var newSuperUser = await userManager.FindByEmailAsync("info@mrccollective");
            if (oldSuperUser is not null)
            {
                if (newSuperUser is null)
                {
                    oldSuperUser.UserName = "info@mrccollective";
                    oldSuperUser.Email = "info@mrccollective";
                    oldSuperUser.NormalizedUserName = userManager.NormalizeName(oldSuperUser.UserName);
                    oldSuperUser.NormalizedEmail = userManager.NormalizeEmail(oldSuperUser.Email);
                    await userManager.UpdateAsync(oldSuperUser);
                    newSuperUser = oldSuperUser;
                }
                else if (!await userManager.IsInRoleAsync(oldSuperUser, ApplicationRoles.SuperUser))
                {
                    await userManager.AddToRoleAsync(oldSuperUser, ApplicationRoles.SuperUser);
                }
            }

            if (newSuperUser is not null && !await userManager.IsInRoleAsync(newSuperUser, ApplicationRoles.SuperUser))
            {
                await userManager.AddToRoleAsync(newSuperUser, ApplicationRoles.SuperUser);
            }
        }
    }
}
