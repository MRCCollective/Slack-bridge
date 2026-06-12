using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.SlackCommandRoutes;

public sealed class CreateModel(
    SlackBridgeDbContext dbContext,
    IApiKeySecretProtector secretProtector,
    ICustomerInstanceContext customerInstanceContext) : PageModel
{
    [BindProperty]
    public RouteInput Input { get; set; } = new();

    public SelectList ProjectOptions { get; private set; } = new(Array.Empty<object>());

    public async Task OnGetAsync(int? projectId, CancellationToken cancellationToken)
    {
        if (projectId.HasValue)
        {
            Input.ProjectId = projectId.Value;
        }

        await LoadProjectsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadProjectsAsync(cancellationToken);
            return Page();
        }

        var projectExists = await dbContext.Projects.AnyAsync(
            project => project.Id == Input.ProjectId &&
                project.CustomerInstanceId == customerInstanceContext.CustomerInstanceId,
            cancellationToken);
        if (!projectExists)
        {
            return NotFound();
        }

        var route = new SlackCommandRoute
        {
            CustomerInstanceId = customerInstanceContext.CustomerInstanceId,
            ProjectId = Input.ProjectId,
            IsActive = Input.IsActive,
            SlackCommand = NormalizeCommand(Input.SlackCommand),
            EncryptedSlackSigningSecret = secretProtector.Protect(Input.SlackSigningSecret),
            DownstreamUrl = Input.DownstreamUrl.Trim(),
            DownstreamAuthHeaderName = Input.DownstreamAuthHeaderName.Trim(),
            EncryptedDownstreamAuthSecret = string.IsNullOrWhiteSpace(Input.DownstreamAuthSecret)
                ? null
                : secretProtector.Protect(Input.DownstreamAuthSecret),
            AllowedTeamId = string.IsNullOrWhiteSpace(Input.AllowedTeamId) ? null : Input.AllowedTeamId.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.SlackCommandRoutes.Add(route);
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToPage("Index");
    }

    private async Task LoadProjectsAsync(CancellationToken cancellationToken)
    {
        var projects = await dbContext.Projects
            .Where(project => project.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);
        ProjectOptions = new SelectList(projects, "Id", "Name", Input.ProjectId);
    }

    private static string NormalizeCommand(string command) => command.Trim().ToLowerInvariant();

    public sealed class RouteInput
    {
        [Display(Name = "Slack bot")]
        public int ProjectId { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Slash command")]
        [Required, MaxLength(80)]
        [RegularExpression("^/[A-Za-z0-9_-]+$", ErrorMessage = "Use a Slack slash command such as /shoppingtajm.")]
        public string SlackCommand { get; set; } = string.Empty;

        [Display(Name = "Slack signing secret")]
        [Required, MaxLength(500)]
        [DataType(DataType.Password)]
        public string SlackSigningSecret { get; set; } = string.Empty;

        [Display(Name = "Downstream URL")]
        [Required, Url, MaxLength(2048)]
        public string DownstreamUrl { get; set; } = string.Empty;

        [Display(Name = "Downstream auth header")]
        [Required, MaxLength(120)]
        [RegularExpression("^[A-Za-z0-9][A-Za-z0-9_-]*$", ErrorMessage = "Use a simple HTTP header name such as x-slackbridge-secret.")]
        public string DownstreamAuthHeaderName { get; set; } = "x-slackbridge-secret";

        [Display(Name = "Downstream auth secret")]
        [MaxLength(500)]
        [DataType(DataType.Password)]
        public string? DownstreamAuthSecret { get; set; }

        [Display(Name = "Allowed Slack team ID")]
        [MaxLength(80)]
        public string? AllowedTeamId { get; set; }
    }
}
