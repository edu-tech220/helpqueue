using System.Net;
using System.Net.Http.Json;
using HelpQueue.Api.Contracts.Agents;
using HelpQueue.Api.Contracts.Categories;
using HelpQueue.Api.Contracts.Common;
using HelpQueue.Api.Contracts.Tickets;
using HelpQueue.Api.Domain.Enums;
using HelpQueue.Api.Tests.Infrastructure;
using Xunit;

namespace HelpQueue.Api.Tests.Api;

public class TicketsApiTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public TicketsApiTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<int> GetAnyCategoryIdAsync(HttpClient client)
    {
        var categories = await client.GetFromJsonAsync<List<CategoryResponse>>("/api/categories");
        return categories!.First().Id;
    }

    [Fact]
    public async Task CreateTicket_AsCustomer_IsAutoAssignedAndComputesSla()
    {
        var client = await _factory.LoginAsAsync("customer@helpqueue.dev");
        var categoryId = await GetAnyCategoryIdAsync(client);

        var response = await client.PostAsJsonAsync("/api/tickets",
            new { title = "Laptop won't boot", description = "Black screen on power-up", categoryId, priority = "High" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>(TestJson.Options);
        Assert.NotNull(ticket);
        Assert.NotNull(ticket!.AssignedAgentName);
        Assert.Equal(TicketStatus.Assigned, ticket.Status);
        Assert.True(ticket.ResolutionDueAt > ticket.ResponseDueAt);
    }

    [Fact]
    public async Task CreateTicket_AsAgent_IsForbidden()
    {
        var client = await _factory.LoginAsAsync("agent1@helpqueue.dev");
        var categoryId = await GetAnyCategoryIdAsync(client);

        var response = await client.PostAsJsonAsync("/api/tickets",
            new { title = "x", description = "y", categoryId, priority = "Low" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Agent_CannotSeeATicketNotAssignedToThem()
    {
        var owner = await _factory.LoginAsAsync("customer@helpqueue.dev");
        var categoryId = await GetAnyCategoryIdAsync(owner);
        var created = await owner.PostAsJsonAsync("/api/tickets", new { title = "Private issue", description = "d", categoryId, priority = "Low" });
        var ticket = await created.Content.ReadFromJsonAsync<TicketResponse>(TestJson.Options);

        var unrelatedAgent = await _factory.LoginAsAsync(ticket!.AssignedAgentName == "Grace Agent" ? "agent2@helpqueue.dev" : "agent1@helpqueue.dev");
        var getResponse = await unrelatedAgent.GetAsync($"/api/tickets/{ticket.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_WithStaleRowVersion_ReturnsConflict()
    {
        var customer = await _factory.LoginAsAsync("customer@helpqueue.dev");
        var categoryId = await GetAnyCategoryIdAsync(customer);
        var created = await customer.PostAsJsonAsync("/api/tickets", new { title = "Printer jam", description = "d", categoryId, priority = "Medium" });
        var ticket = (await created.Content.ReadFromJsonAsync<TicketResponse>(TestJson.Options))!;

        var agent = await _factory.LoginAsAsync(AgentEmailFor(ticket));

        var firstChange = await agent.PatchAsJsonAsync($"/api/tickets/{ticket.Id}/status",
            new { status = "InProgress", rowVersion = ticket.RowVersion });
        Assert.Equal(HttpStatusCode.OK, firstChange.StatusCode);

        // Reusing the original (now stale) RowVersion must be rejected.
        var staleChange = await agent.PatchAsJsonAsync($"/api/tickets/{ticket.Id}/status",
            new { status = "OnHold", rowVersion = ticket.RowVersion });

        Assert.Equal(HttpStatusCode.Conflict, staleChange.StatusCode);
    }

    [Fact]
    public async Task InternalComment_IsHiddenFromTheRequester()
    {
        var customer = await _factory.LoginAsAsync("customer@helpqueue.dev");
        var categoryId = await GetAnyCategoryIdAsync(customer);
        var created = await customer.PostAsJsonAsync("/api/tickets", new { title = "VPN issue", description = "d", categoryId, priority = "Low" });
        var ticket = await created.Content.ReadFromJsonAsync<TicketResponse>(TestJson.Options);

        var agent = await _factory.LoginAsAsync(AgentEmailFor(ticket!));
        var commentResponse = await agent.PostAsJsonAsync($"/api/tickets/{ticket!.Id}/comments",
            new { body = "Internal: escalate to networking team", isInternal = true });
        commentResponse.EnsureSuccessStatusCode();

        var customerView = await customer.GetFromJsonAsync<TicketResponse>($"/api/tickets/{ticket.Id}", TestJson.Options);
        var agentView = await agent.GetFromJsonAsync<TicketResponse>($"/api/tickets/{ticket.Id}", TestJson.Options);

        Assert.DoesNotContain(customerView!.Comments, c => c.IsInternal);
        Assert.Contains(agentView!.Comments, c => c.IsInternal);
    }

    [Fact]
    public async Task Reopen_OutsideThePolicyRules_ReturnsConflict()
    {
        var customer = await _factory.LoginAsAsync("customer@helpqueue.dev");
        var categoryId = await GetAnyCategoryIdAsync(customer);
        var created = await customer.PostAsJsonAsync("/api/tickets", new { title = "Still open", description = "d", categoryId, priority = "Low" });
        var ticket = await created.Content.ReadFromJsonAsync<TicketResponse>(TestJson.Options);

        // The ticket is still New/Assigned, not Resolved/Closed, so reopening it must fail.
        var reopenResponse = await customer.PostAsync($"/api/tickets/{ticket!.Id}/reopen", null);

        Assert.Equal(HttpStatusCode.Conflict, reopenResponse.StatusCode);
    }

    [Fact]
    public async Task List_ScopesResultsToTheCallersRole()
    {
        var customer = await _factory.LoginAsAsync("customer@helpqueue.dev");
        var categoryId = await GetAnyCategoryIdAsync(customer);
        await customer.PostAsJsonAsync("/api/tickets", new { title = "Scoped ticket", description = "d", categoryId, priority = "Low" });

        var page = await customer.GetFromJsonAsync<PagedResult<TicketListItemResponse>>("/api/tickets?page=1&pageSize=50", TestJson.Options);

        Assert.NotNull(page);
        Assert.Contains(page!.Items, t => t.Title == "Scoped ticket");
    }

    [Fact]
    public async Task Reassign_AsAdmin_MovesWorkloadBetweenAgents()
    {
        var customer = await _factory.LoginAsAsync("customer@helpqueue.dev");
        var categoryId = await GetAnyCategoryIdAsync(customer);
        var created = await customer.PostAsJsonAsync("/api/tickets", new { title = "Needs reassignment", description = "d", categoryId, priority = "Low" });
        var ticket = await created.Content.ReadFromJsonAsync<TicketResponse>(TestJson.Options);

        var admin = await _factory.LoginAsAsync("admin@helpqueue.dev");
        var workload = await admin.GetFromJsonAsync<List<AgentWorkloadResponse>>("/api/agents/workload");
        var targetAgent = workload!.First(a => a.DisplayName != ticket!.AssignedAgentName);

        var reassignResponse = await admin.PostAsJsonAsync($"/api/tickets/{ticket!.Id}/reassign",
            new { agentId = targetAgent.Id, rowVersion = ticket.RowVersion });

        reassignResponse.EnsureSuccessStatusCode();
        var updated = await reassignResponse.Content.ReadFromJsonAsync<TicketResponse>(TestJson.Options);
        Assert.Equal(targetAgent.DisplayName, updated!.AssignedAgentName);
    }

    [Fact]
    public async Task AgentWorkload_IsNotVisibleToCustomers()
    {
        var customer = await _factory.LoginAsAsync("customer@helpqueue.dev");
        var response = await customer.GetAsync("/api/agents/workload");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static string AgentEmailFor(TicketResponse ticket) =>
        ticket.AssignedAgentName == "Grace Agent" ? "agent1@helpqueue.dev" : "agent2@helpqueue.dev";
}
