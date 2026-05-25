using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.Projects;

public sealed class DetailsModel(
    SlackBridgeDbContext dbContext,
    ICustomerInstanceContext customerInstanceContext,
    IApiKeySecretProtector apiKeySecretProtector,
    IEventDefinitionTestService eventDefinitionTestService,
    ILocalClock localClock) : PageModel
{
    public int ProjectId { get; private set; }
    public string ProjectName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Created { get; private set; } = string.Empty;
    public bool HasSlackWebhook { get; private set; }
    public string? CreatedKey { get; private set; }
    public string? StatusMessage { get; private set; }
    public IReadOnlyList<ApiKeyRow> ApiKeys { get; private set; } = [];
    public IReadOnlyList<EventDefinitionRow> EventDefinitions { get; private set; } = [];
    public IReadOnlyList<LogRow> Logs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, string? createdKey, string? statusMessage, CancellationToken cancellationToken)
    {
        CreatedKey = createdKey;
        StatusMessage = statusMessage;
        return await LoadAsync(id, cancellationToken) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostTestEventAsync(int id, int eventDefinitionId, CancellationToken cancellationToken)
    {
        var definition = await dbContext.EventDefinitions
            .Include(eventDefinition => eventDefinition.Project)
            .SingleOrDefaultAsync(eventDefinition =>
                eventDefinition.Id == eventDefinitionId &&
                eventDefinition.ProjectId == id &&
                eventDefinition.CustomerInstanceId == customerInstanceContext.CustomerInstanceId,
                cancellationToken);

        if (definition is null)
        {
            return NotFound();
        }

        EventDefinitionTestResult result;
        try
        {
            result = await eventDefinitionTestService.SendAsync(definition, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return RedirectToPage("Details", new
            {
                id,
                statusMessage = $"Test send for '{definition.Key}' failed: {exception.Message}"
            });
        }

        return RedirectToPage("Details", new
        {
            id,
            statusMessage = $"{result.Message} Log #{result.LogId}."
        });
    }

    private async Task<bool> LoadAsync(int id, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(project =>
                project.Id == id &&
                project.CustomerInstanceId == customerInstanceContext.CustomerInstanceId,
                cancellationToken);

        if (project is null)
        {
            return false;
        }

        ProjectId = project.Id;
        ProjectName = project.Name;
        Description = project.Description;
        Created = localClock.Format(project.CreatedAtUtc);
        HasSlackWebhook = !string.IsNullOrWhiteSpace(project.SlackWebhookUrl);

        var apiKeys = await dbContext.ApiKeys
            .Where(apiKey => apiKey.ProjectId == id && apiKey.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderBy(apiKey => apiKey.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        ApiKeys = apiKeys
            .Select(apiKey => new ApiKeyRow(
                apiKey.Id,
                apiKey.Name,
                apiKey.KeyPrefix,
                apiKeySecretProtector.Unprotect(apiKey.EncryptedKey),
                apiKey.IsActive,
                apiKey.LastUsedAtUtc is null ? "" : localClock.Format(apiKey.LastUsedAtUtc.Value)))
            .ToList();

        EventDefinitions = await dbContext.EventDefinitions
            .Where(definition => definition.ProjectId == id && definition.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderBy(definition => definition.Key)
            .Select(definition => new EventDefinitionRow(
                definition.Id,
                definition.Key,
                definition.UseCustomSlackWebhook,
                definition.IsActive))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var logs = await dbContext.EventLogs
            .Where(log => log.ProjectId == id && log.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderByDescending(log => log.CreatedAtUtc)
            .Take(20)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Logs = logs
            .Select(log => new LogRow(
                localClock.Format(log.CreatedAtUtc),
                log.EventKey,
                log.Status.ToString(),
                log.SlackStatusCode,
                log.ResultMessage))
            .ToList();

        return true;
    }

    public sealed record ApiKeyRow(
        int Id,
        string Name,
        string KeyPrefix,
        string? ApiKey,
        bool IsActive,
        string LastUsed);

    public sealed record EventDefinitionRow(
        int Id,
        string Key,
        bool UsesCustomWebhook,
        bool IsActive);

    public sealed record LogRow(
        string Created,
        string EventKey,
        string Status,
        int? SlackStatusCode,
        string? ResultMessage);
}
