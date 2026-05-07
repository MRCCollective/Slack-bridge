using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;

namespace SlackBridge.Web.Services;

public interface IApiKeyValidator
{
    Task<ApiKey?> ValidateAsync(string apiKey, CancellationToken cancellationToken);
}

public sealed class ApiKeyValidator(
    SlackBridgeDbContext dbContext,
    IApiKeyGenerator apiKeyGenerator) : IApiKeyValidator
{
    public async Task<ApiKey?> ValidateAsync(string apiKey, CancellationToken cancellationToken)
    {
        var hash = apiKeyGenerator.Hash(apiKey);
        var entity = await dbContext.ApiKeys
            .Include(key => key.Project)
            .SingleOrDefaultAsync(key => key.KeyHash == hash && key.IsActive, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.LastUsedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
