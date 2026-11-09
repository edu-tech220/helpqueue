using HelpQueue.Api.Data;
using HelpQueue.Api.Domain.Entities;
using HelpQueue.Api.Domain.Enums;
using HelpQueue.Api.Services;
using HelpQueue.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HelpQueue.Api.Tests.Services;

public class TicketAssignmentServiceTests : IDisposable
{
    private readonly SqliteContextFactory _factory = new();

    [Fact]
    public async Task AssignLeastLoadedAgentAsync_PicksTheAgentWithFewestOpenTickets()
    {
        using (var seed = _factory.CreateContext())
        {
            seed.Users.AddRange(
                new User { Email = "busy@test", DisplayName = "Busy", PasswordHash = "x", Role = UserRole.Agent, OpenTicketCount = 3 },
                new User { Email = "free@test", DisplayName = "Free", PasswordHash = "x", Role = UserRole.Agent, OpenTicketCount = 0 });
            seed.Categories.Add(new Category { Name = "General" });
            await seed.SaveChangesAsync();
        }

        using var db = _factory.CreateContext();
        var ticket = await SeedUnassignedTicketAsync(db);
        var service = new TicketAssignmentService(db);

        var assigned = await service.AssignLeastLoadedAgentAsync(ticket);

        Assert.NotNull(assigned);
        Assert.Equal("Free", assigned!.DisplayName);
        Assert.Equal(1, assigned.OpenTicketCount);
        Assert.Equal(TicketStatus.Assigned, ticket.Status);
    }

    [Fact]
    public async Task AssignLeastLoadedAgentAsync_ReturnsNull_WhenNoActiveAgentExists()
    {
        using var db = _factory.CreateContext();
        db.Categories.Add(new Category { Name = "General" });
        await db.SaveChangesAsync();
        var ticket = await SeedUnassignedTicketAsync(db);
        var service = new TicketAssignmentService(db);

        var assigned = await service.AssignLeastLoadedAgentAsync(ticket);

        Assert.Null(assigned);
        Assert.Equal(TicketStatus.New, ticket.Status);
    }

    [Fact]
    public async Task AssignLeastLoadedAgentAsync_RetriesOnConcurrencyConflict_WithoutLosingAnUpdate()
    {
        int agentId;
        using (var seed = _factory.CreateContext())
        {
            var agent = new User { Email = "agent@test", DisplayName = "Agent", PasswordHash = "x", Role = UserRole.Agent };
            seed.Users.Add(agent);
            seed.Categories.Add(new Category { Name = "General" });
            await seed.SaveChangesAsync();
            agentId = agent.Id;
        }

        using var contextA = _factory.CreateContext();
        using var contextB = _factory.CreateContext();

        var ticketA = await SeedUnassignedTicketAsync(contextA);
        var ticketB = await SeedUnassignedTicketAsync(contextB);

        // contextB "warms up" its tracked copy of the agent before contextA commits,
        // simulating a second in-flight request that already read the old workload.
        await contextB.Users.OrderBy(u => u.OpenTicketCount).FirstAsync();

        var serviceA = new TicketAssignmentService(contextA);
        var firstAssignee = await serviceA.AssignLeastLoadedAgentAsync(ticketA);
        Assert.NotNull(firstAssignee);

        var serviceB = new TicketAssignmentService(contextB);
        var secondAssignee = await serviceB.AssignLeastLoadedAgentAsync(ticketB);
        Assert.NotNull(secondAssignee);

        using var verify = _factory.CreateContext();
        var agentAfter = await verify.Users.SingleAsync(u => u.Id == agentId);
        Assert.Equal(2, agentAfter.OpenTicketCount);
    }

    private static async Task<Ticket> SeedUnassignedTicketAsync(HelpQueueDbContext db)
    {
        var category = await db.Categories.FirstAsync();
        var requester = db.Users.Local.FirstOrDefault(u => u.Role == UserRole.Customer);
        if (requester is null)
        {
            requester = new User { Email = $"customer{Guid.NewGuid():N}@test", DisplayName = "Customer", PasswordHash = "x", Role = UserRole.Customer };
            db.Users.Add(requester);
            await db.SaveChangesAsync();
        }

        var ticket = new Ticket
        {
            Title = "Needs help",
            Description = "Something is broken",
            Priority = TicketPriority.Medium,
            CategoryId = category.Id,
            RequesterId = requester.Id,
            CreatedAt = DateTime.UtcNow,
            ResponseDueAt = DateTime.UtcNow.AddHours(4),
            ResolutionDueAt = DateTime.UtcNow.AddDays(1)
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        return ticket;
    }

    public void Dispose() => _factory.Dispose();
}
