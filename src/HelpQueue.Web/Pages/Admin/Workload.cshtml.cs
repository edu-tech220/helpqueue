using HelpQueue.Web.Models.Api;
using HelpQueue.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HelpQueue.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class WorkloadModel : PageModel
{
    private readonly ApiClient _api;

    public WorkloadModel(ApiClient api)
    {
        _api = api;
    }

    public List<AgentWorkload> Agents { get; set; } = new();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Agents = await _api.GetAgentWorkloadAsync(ct);
    }
}
