using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.ApiKeys;

public sealed class CreateModel(
    SlackBridgeDbContext dbContext,
    IApiKeyGenerator apiKeyGenerator,
    IApiKeySecretProtector apiKeySecretProtector,
    ICustomerInstanceContext customerInstanceContext,
    IUsageService usageService) : PageModel
{
    [BindProperty]
    public ApiKeyInput Input { get; set; } = new();

    public SelectList ProjectOptions { get; private set; } = new(Array.Empty<object>());

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadProjectsAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadProjectsAsync(cancellationToken);
            return Page();
        }

        try
        {
            await usageService.EnsureApiKeyLimitAsync(cancellationToken);
        }
        catch (PlanLimitExceededException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadProjectsAsync(cancellationToken);
            return Page();
        }

        var rawKey = apiKeyGenerator.Generate();
        dbContext.ApiKeys.Add(new ApiKey
        {
            CustomerInstanceId = customerInstanceContext.CustomerInstanceId,
            ProjectId = Input.ProjectId,
            Name = Input.Name,
            KeyHash = apiKeyGenerator.Hash(rawKey),
            KeyPrefix = apiKeyGenerator.Prefix(rawKey),
            EncryptedKey = apiKeySecretProtector.Protect(rawKey)
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToPage("Index", new { createdKey = rawKey });
    }

    private async Task LoadProjectsAsync(CancellationToken cancellationToken)
    {
        var projects = await dbContext.Projects
            .Where(project => project.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);
        ProjectOptions = new SelectList(projects, "Id", "Name");
    }

    public sealed class ApiKeyInput
    {
        [Display(Name = "Project")]
        public int ProjectId { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;
    }
}
