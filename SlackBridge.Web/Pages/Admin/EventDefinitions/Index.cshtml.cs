using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.EventDefinitions;

public sealed class IndexModel(
    SlackBridgeDbContext dbContext,
    IEventDefinitionTestService eventDefinitionTestService,
    ICustomerInstanceContext customerInstanceContext) : PageModel
{
    public IReadOnlyList<EventDefinition> EventDefinitions { get; private set; } = [];
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(string? statusMessage, CancellationToken cancellationToken)
    {
        StatusMessage = statusMessage;
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostTestAsync(int id, CancellationToken cancellationToken)
    {
        var definition = await dbContext.EventDefinitions
            .Include(eventDefinition => eventDefinition.Project)
            .SingleOrDefaultAsync(eventDefinition =>
                eventDefinition.Id == id &&
                eventDefinition.CustomerInstanceId == customerInstanceContext.CustomerInstanceId,
                cancellationToken);

        if (definition is null)
        {
            return NotFound();
        }

        try
        {
            var result = await eventDefinitionTestService.SendAsync(definition, cancellationToken);
            return RedirectToPage("Index", new { statusMessage = $"{result.Message} Log #{result.LogId}." });
        }
        catch (InvalidOperationException exception)
        {
            return RedirectToPage("Index", new { statusMessage = $"Test send for '{definition.Key}' failed: {exception.Message}" });
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        EventDefinitions = await dbContext.EventDefinitions
            .Include(eventDefinition => eventDefinition.Project)
            .Where(eventDefinition => eventDefinition.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderBy(eventDefinition => eventDefinition.Project!.Name)
            .ThenBy(eventDefinition => eventDefinition.Key)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
