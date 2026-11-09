using System.Net.Http.Headers;
using System.Net.Http.Json;
using HelpQueue.Web.Models.Api;

namespace HelpQueue.Web.Services;

/// <summary>Thin wrapper over an IHttpClientFactory-created client that talks to the HelpQueue API.</summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _http = httpClientFactory.CreateClient("HelpQueueApi");
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LoginResult?> LoginAsync(string email, string password, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password }, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LoginResult>(cancellationToken: ct);
    }

    public async Task<List<Category>> GetCategoriesAsync(CancellationToken ct)
    {
        AttachToken();
        var response = await _http.GetAsync("api/categories", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Category>>(cancellationToken: ct) ?? new();
    }

    public async Task<PagedResult<TicketListItem>> GetTicketsAsync(string? status, string? priority, int page, int pageSize, CancellationToken ct)
    {
        AttachToken();
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrEmpty(status)) query.Add($"status={status}");
        if (!string.IsNullOrEmpty(priority)) query.Add($"priority={priority}");

        var response = await _http.GetAsync($"api/tickets?{string.Join('&', query)}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResult<TicketListItem>>(cancellationToken: ct)
            ?? new PagedResult<TicketListItem>(new(), page, pageSize, 0, 0);
    }

    public async Task<TicketDetail?> GetTicketAsync(int id, CancellationToken ct)
    {
        AttachToken();
        var response = await _http.GetAsync($"api/tickets/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TicketDetail>(cancellationToken: ct);
    }

    public async Task<(bool Success, string? Error)> CreateTicketAsync(string title, string description, int categoryId, string priority, CancellationToken ct)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync("api/tickets", new { title, description, categoryId, priority }, ct);
        return response.IsSuccessStatusCode ? (true, null) : (false, await ReadErrorAsync(response, ct));
    }

    public async Task<(bool Success, string? Error)> ChangeStatusAsync(int id, string status, byte[] rowVersion, CancellationToken ct)
    {
        AttachToken();
        var request = new HttpRequestMessage(HttpMethod.Patch, $"api/tickets/{id}/status")
        {
            Content = JsonContent.Create(new { status, rowVersion })
        };
        var response = await _http.SendAsync(request, ct);
        return response.IsSuccessStatusCode ? (true, null) : (false, await ReadErrorAsync(response, ct));
    }

    public async Task<(bool Success, string? Error)> ReopenAsync(int id, CancellationToken ct)
    {
        AttachToken();
        var response = await _http.PostAsync($"api/tickets/{id}/reopen", null, ct);
        return response.IsSuccessStatusCode ? (true, null) : (false, await ReadErrorAsync(response, ct));
    }

    public async Task<(bool Success, string? Error)> AddCommentAsync(int id, string body, bool isInternal, CancellationToken ct)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync($"api/tickets/{id}/comments", new { body, isInternal }, ct);
        return response.IsSuccessStatusCode ? (true, null) : (false, await ReadErrorAsync(response, ct));
    }

    public async Task<(bool Success, string? Error)> ReassignAsync(int id, int agentId, byte[] rowVersion, CancellationToken ct)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync($"api/tickets/{id}/reassign", new { agentId, rowVersion }, ct);
        return response.IsSuccessStatusCode ? (true, null) : (false, await ReadErrorAsync(response, ct));
    }

    public async Task<List<AgentWorkload>> GetAgentWorkloadAsync(CancellationToken ct)
    {
        AttachToken();
        var response = await _http.GetAsync("api/agents/workload", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AgentWorkload>>(cancellationToken: ct) ?? new();
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsLite>(cancellationToken: ct);
            return problem?.Title ?? $"Request failed with status {(int)response.StatusCode}.";
        }
        catch
        {
            return $"Request failed with status {(int)response.StatusCode}.";
        }
    }

    private void AttachToken()
    {
        var token = _httpContextAccessor.HttpContext?.User.FindFirst("access_token")?.Value;
        _http.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    private record ProblemDetailsLite(string? Title);
}
