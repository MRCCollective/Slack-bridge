using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.SlackCommandRoutes;

public sealed class EditModel(
    SlackBridgeDbContext dbContext,
    IApiKeySecretProtector secretProtector,
    ICustomerInstanceContext customerInstanceContext) : PageModel
{
    [BindProperty]
    public RouteInput Input { get; set; } = new();

    public SelectList ProjectOptions { get; private set; } = new(Array.Empty<object>());
    public bool HasSlackSigningSecret { get; private set; }
    public bool HasDownstreamAuthSecret { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var route = await dbContext.SlackCommandRoutes
            .AsNoTracking()
            .SingleOrDefaultAsync(route =>
                route.Id == id &&
                route.CustomerInstanceId == customerInstanceContext.CustomerInstanceId,
                cancellationToken);

        if (route is null)
        {
            return NotFound();
        }

        Input = new RouteInput
        {
            Id = route.Id,
            ProjectId = route.ProjectId,
            IsActive = route.IsActive,
            SlackCommand = route.SlackCommand,
            DownstreamUrl = route.DownstreamUrl,
            DownstreamAuthHeaderName = route.DownstreamAuthHeaderName,
            AllowedTeamId = route.AllowedTeamId
        };
        HasSlackSigningSecret = !string.IsNullOrWhiteSpace(route.EncryptedSlackSigningSecret);
        HasDownstreamAuthSecret = !string.IsNullOrWhiteSpace(route.EncryptedDownstreamAuthSecret);

        await LoadProjectsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var route = await dbContext.SlackCommandRoutes
            .SingleOrDefaultAsync(route =>
                route.Id == Input.Id &&
                route.CustomerInstanceId == customerInstanceContext.CustomerInstanceId,
                cancellationToken);

        if (route is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(Input.SlackSigningSecret) &&
            string.IsNullOrWhiteSpace(route.EncryptedSlackSigningSecret))
        {
            ModelState.AddModelError("Input.SlackSigningSecret", "Enter a Slack signing secret.");
        }

        if (!ModelState.IsValid)
        {
            HasSlackSigningSecret = !string.IsNullOrWhiteSpace(route.EncryptedSlackSigningSecret);
            HasDownstreamAuthSecret = !string.IsNullOrWhiteSpace(route.EncryptedDownstreamAuthSecret);
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

        route.ProjectId = Input.ProjectId;
        route.IsActive = Input.IsActive;
        route.SlackCommand = Input.SlackCommand.Trim().ToLowerInvariant();
        route.DownstreamUrl = Input.DownstreamUrl.Trim();
        route.DownstreamAuthHeaderName = Input.DownstreamAuthHeaderName.Trim();
        route.AllowedTeamId = string.IsNullOrWhiteSpace(Input.AllowedTeamId) ? null : Input.AllowedTeamId.Trim();
        route.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(Input.SlackSigningSecret))
        {
            route.EncryptedSlackSigningSecret = secretProtector.Protect(Input.SlackSigningSecret);
        }

        if (Input.ClearDownstreamAuthSecret)
        {
            route.EncryptedDownstreamAuthSecret = null;
        }
        else if (!string.IsNullOrWhiteSpace(Input.DownstreamAuthSecret))
        {
            route.EncryptedDownstreamAuthSecret = secretProtector.Protect(Input.DownstreamAuthSecret);
        }

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

    public sealed class RouteInput
    {
        public int Id { get; set; }

        [Display(Name = "Slack bot")]
        public int ProjectId { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Slash command")]
        [Required, MaxLength(80)]
        [RegularExpression("^/[A-Za-z0-9_-]+$", ErrorMessage = "Use a Slack slash command such as /shoppingtajm.")]
        public string SlackCommand { get; set; } = string.Empty;

        [Display(Name = "Replace Slack signing secret")]
        [MaxLength(500)]
        [DataType(DataType.Password)]
        public string? SlackSigningSecret { get; set; }

        [Display(Name = "Downstream URL")]
        [Required, Url, MaxLength(2048)]
        public string DownstreamUrl { get; set; } = string.Empty;

        [Display(Name = "Downstream auth header")]
        [Required, MaxLength(120)]
        [RegularExpression("^[A-Za-z0-9][A-Za-z0-9_-]*$", ErrorMessage = "Use a simple HTTP header name such as x-slackbridge-secret.")]
        public string DownstreamAuthHeaderName { get; set; } = "x-slackbridge-secret";

        [Display(Name = "Replace downstream auth secret")]
        [MaxLength(500)]
        [DataType(DataType.Password)]
        public string? DownstreamAuthSecret { get; set; }

        [Display(Name = "Clear downstream auth secret")]
        public bool ClearDownstreamAuthSecret { get; set; }

        [Display(Name = "Allowed Slack team ID")]
        [MaxLength(80)]
        public string? AllowedTeamId { get; set; }
    }
}
