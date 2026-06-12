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
            await EnsureMariaDbSchemaAsync(configuration, dbContext);
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

    private static async Task EnsureMariaDbSchemaAsync(IConfiguration configuration, SlackBridgeDbContext dbContext)
    {
        if (!string.Equals(configuration["Database:Provider"], "MariaDb", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS `SlackCommandRoutes` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `CustomerInstanceId` int NOT NULL,
                `ProjectId` int NOT NULL,
                `IsActive` tinyint(1) NOT NULL,
                `SlackCommand` varchar(80) NOT NULL,
                `EncryptedSlackSigningSecret` longtext NOT NULL,
                `DownstreamUrl` varchar(2048) NOT NULL,
                `DownstreamAuthHeaderName` varchar(120) NOT NULL,
                `EncryptedDownstreamAuthSecret` longtext NULL,
                `AllowedTeamId` varchar(80) NULL,
                `CreatedAtUtc` datetime(6) NOT NULL,
                `UpdatedAtUtc` datetime(6) NOT NULL,
                PRIMARY KEY (`Id`),
                KEY `IX_SlackCommandRoutes_CustomerInstanceId` (`CustomerInstanceId`),
                KEY `IX_SlackCommandRoutes_ProjectId_SlackCommand` (`ProjectId`, `SlackCommand`),
                CONSTRAINT `FK_SlackCommandRoutes_CustomerInstances_CustomerInstanceId`
                    FOREIGN KEY (`CustomerInstanceId`) REFERENCES `CustomerInstances` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_SlackCommandRoutes_Projects_ProjectId`
                    FOREIGN KEY (`ProjectId`) REFERENCES `Projects` (`Id`) ON DELETE CASCADE
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS `SlackCommandLogs` (
                `Id` bigint NOT NULL AUTO_INCREMENT,
                `CustomerInstanceId` int NOT NULL,
                `ProjectId` int NULL,
                `SlackCommandRouteId` int NULL,
                `Command` varchar(80) NULL,
                `TeamId` varchar(80) NULL,
                `ChannelId` varchar(80) NULL,
                `UserId` varchar(80) NULL,
                `DownstreamStatusCode` int NULL,
                `Status` int NOT NULL,
                `ResultMessage` varchar(1000) NULL,
                `CreatedAtUtc` datetime(6) NOT NULL,
                PRIMARY KEY (`Id`),
                KEY `IX_SlackCommandLogs_Command_TeamId` (`Command`, `TeamId`),
                KEY `IX_SlackCommandLogs_CreatedAtUtc` (`CreatedAtUtc`),
                KEY `IX_SlackCommandLogs_CustomerInstanceId` (`CustomerInstanceId`),
                KEY `IX_SlackCommandLogs_ProjectId` (`ProjectId`),
                KEY `IX_SlackCommandLogs_SlackCommandRouteId` (`SlackCommandRouteId`),
                CONSTRAINT `FK_SlackCommandLogs_CustomerInstances_CustomerInstanceId`
                    FOREIGN KEY (`CustomerInstanceId`) REFERENCES `CustomerInstances` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_SlackCommandLogs_Projects_ProjectId`
                    FOREIGN KEY (`ProjectId`) REFERENCES `Projects` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_SlackCommandLogs_SlackCommandRoutes_SlackCommandRouteId`
                    FOREIGN KEY (`SlackCommandRouteId`) REFERENCES `SlackCommandRoutes` (`Id`) ON DELETE SET NULL
            );
            """);
    }
}
