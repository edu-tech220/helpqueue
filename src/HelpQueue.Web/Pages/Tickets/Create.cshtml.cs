using System.ComponentModel.DataAnnotations;
using HelpQueue.Web.Models.Api;
using HelpQueue.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HelpQueue.Web.Pages.Tickets;

[Authorize(Roles = "Customer,Admin")]
public class CreateModel : PageModel
{
    private readonly ApiClient _api;

    public CreateModel(ApiClient api)
    {
        _api = api;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<Category> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(4000)]
        public string Description { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public string Priority { get; set; } = "Medium";
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Categories = await _api.GetCategoriesAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        Categories = await _api.GetCategoriesAsync(ct);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var (success, error) = await _api.CreateTicketAsync(Input.Title, Input.Description, Input.CategoryId, Input.Priority, ct);
        if (!success)
        {
            ErrorMessage = error;
            return Page();
        }

        return RedirectToPage("/Tickets/Index");
    }
}
