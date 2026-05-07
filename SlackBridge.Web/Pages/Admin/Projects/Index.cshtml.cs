using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

namespace SlackBridge.Web.Pages.Admin.Projects;

public sealed class IndexModel(SlackBridgeDbContext dbContext, ICustomerInstanceContext customerInstanceContext) : PageModel
{
    public IReadOnlyList<Project> Projects { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Projects = await dbContext.Projects
            .Where(project => project.CustomerInstanceId == customerInstanceContext.CustomerInstanceId)
            .OrderBy(project => project.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
