using HelpQueue.Web.Models.Api;
using HelpQueue.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HelpQueue.Web.Pages.Tickets;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApiClient _api;

    public IndexModel(ApiClient api)
    {
        _api = api;
    }

    public PagedResult<TicketListItem> Tickets { get; set; } = new(new(), 1, 10, 0, 0);

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Priority { get; set; }

    [BindProperty(SupportsGet = true, Name = "Page")]
    public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync(CancellationToken ct)
    {
        Tickets = await _api.GetTicketsAsync(Status, Priority, PageNumber, 10, ct);
    }
}
