using HelpQueue.Web.Models.Api;
using HelpQueue.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HelpQueue.Web.Pages.Tickets;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ApiClient _api;

    public DetailsModel(ApiClient api)
    {
        _api = api;
    }

    public TicketDetail Ticket { get; set; } = default!;
    public List<AgentWorkload> Agents { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public string NewStatus { get; set; } = string.Empty;

    [BindProperty]
    public string CommentBody { get; set; } = string.Empty;

    [BindProperty]
    public bool IsInternalComment { get; set; }

    [BindProperty]
    public int ReassignAgentId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        var ticket = await _api.GetTicketAsync(id, ct);
        if (ticket is null)
        {
            return RedirectToPage("/Tickets/Index");
        }

        Ticket = ticket;

        if (User.IsInRole("Admin"))
        {
            Agents = await _api.GetAgentWorkloadAsync(ct);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostChangeStatusAsync(int id, CancellationToken ct)
    {
        var ticket = await _api.GetTicketAsync(id, ct);
        if (ticket is null)
        {
            return RedirectToPage("/Tickets/Index");
        }

        var (success, error) = await _api.ChangeStatusAsync(id, NewStatus, ticket.RowVersion, ct);
        if (!success)
        {
            ErrorMessage = error;
            Ticket = ticket;
            return Page();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddCommentAsync(int id, CancellationToken ct)
    {
        var (success, error) = await _api.AddCommentAsync(id, CommentBody, IsInternalComment, ct);
        if (!success)
        {
            ErrorMessage = error;
            Ticket = (await _api.GetTicketAsync(id, ct))!;
            return Page();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReopenAsync(int id, CancellationToken ct)
    {
        var (success, error) = await _api.ReopenAsync(id, ct);
        if (!success)
        {
            ErrorMessage = error;
            Ticket = (await _api.GetTicketAsync(id, ct))!;
            return Page();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReassignAsync(int id, CancellationToken ct)
    {
        var ticket = await _api.GetTicketAsync(id, ct);
        if (ticket is null)
        {
            return RedirectToPage("/Tickets/Index");
        }

        var (success, error) = await _api.ReassignAsync(id, ReassignAgentId, ticket.RowVersion, ct);
        if (!success)
        {
            ErrorMessage = error;
            Ticket = ticket;
            Agents = await _api.GetAgentWorkloadAsync(ct);
            return Page();
        }

        return RedirectToPage(new { id });
    }
}
