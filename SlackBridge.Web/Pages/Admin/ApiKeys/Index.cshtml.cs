using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.ApiKeys;

public sealed class IndexModel(
    SlackBridgeDbContext dbContext,
    ICustomerInstanceContext customerInstanceContext,
    IApiKeySecretProtector apiKeySecretProtector) : PageModel
{
    public IReadOnlyList<ApiKeyListItem> ApiKeys { get; private set; } = [];
    public string? CreatedKey { get; private set; }

    public async Task OnGetAsync(string? createdKey, CancellationToken cancellationToken)
    {
        CreatedKey = createdKey;
        var apiKeys = await dbContext.ApiKeys
            .Include(apiKey => apiKey.Project)
            .Where(apiKey => apiKey.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderBy(apiKey => apiKey.Project!.Name)
            .ThenBy(apiKey => apiKey.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        ApiKeys = apiKeys
            .Select(apiKey => new ApiKeyListItem(
                apiKey.Id,
                apiKey.Name,
                apiKey.Project?.Name ?? "",
                apiKey.KeyPrefix,
                apiKeySecretProtector.Unprotect(apiKey.EncryptedKey),
                apiKey.IsActive,
                apiKey.LastUsedAtUtc))
            .ToList();
    }

    public sealed record ApiKeyListItem(
        int Id,
        string Name,
        string ProjectName,
        string KeyPrefix,
        string? ApiKey,
        bool IsActive,
        DateTimeOffset? LastUsedAtUtc);
}
