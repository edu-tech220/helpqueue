using HelpQueue.Api.Domain.Entities;
using HelpQueue.Api.Domain.Enums;
using HelpQueue.Api.Services;
using HelpQueue.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HelpQueue.Api.Tests.Services;

public class TicketWorkflowServiceTests : IDisposable
{
    private readonly SqliteContextFactory _factory = new();

    private static async Task<(int CategoryId, int RequesterId)> SeedReferenceDataAsync(HelpQueue.Api.Data.HelpQueueDbContext db)
    {
        var category = new Category { Name = "General" };
        var requester = new User { Email = $"c{Guid.NewGuid():N}@test", DisplayName = "Customer", PasswordHash = "x", Role = UserRole.Customer };
        db.Categories.Add(category);
        db.Users.Add(requester);
        db.SlaPolicies.AddRange(
            new SlaPolicy { Priority = TicketPriority.Low, ResponseMinutes = 480, ResolutionMinutes = 7200 },
            new SlaPolicy { Priority = TicketPriority.Medium, ResponseMinutes = 240, ResolutionMinutes = 4320 },
            new SlaPolicy { Priority = TicketPriority.High, ResponseMinutes = 60, ResolutionMinutes = 480 },
            new SlaPolicy { Priority = TicketPriority.Critical, ResponseMinutes = 15, ResolutionMinutes = 240 });
        await db.SaveChangesAsync();
        return (category.Id, requester.Id);
    }

    [Fact]
    public async Task CreateTicketAsync_ComputesSlaAndAutoAssignsAnAgent()
    {
        using var db = _factory.CreateContext();
        var (categoryId, requesterId) = await SeedReferenceDataAsync(db);
        db.Users.Add(new User { Email = "agent@test", DisplayName = "Agent", PasswordHash = "x", Role = UserRole.Agent });
        await db.SaveChangesAsync();

        var workflow = new TicketWorkflowService(db, new SlaCalculator(db), new TicketAssignmentService(db));
        var now = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);

        var ticket = await workflow.CreateTicketAsync("Broken printer", "It won't print", categoryId, requesterId, TicketPriority.High, now);

        Assert.Equal(now.AddMinutes(60), ticket.ResponseDueAt);
        Assert.Equal(now.AddMinutes(480), ticket.ResolutionDueAt);
        Assert.Equal(TicketStatus.Assigned, ticket.Status);
        Assert.NotNull(ticket.AssignedAgentId);
    }

    [Fact]
    public async Task CreateTicketAsync_LeavesTicketNew_WhenNoAgentIsAvailable()
    {
        using var db = _factory.CreateContext();
        var (categoryId, requesterId) = await SeedReferenceDataAsync(db);

        var workflow = new TicketWorkflowService(db, new SlaCalculator(db), new TicketAssignmentService(db));
        var ticket = await workflow.CreateTicketAsync("Broken printer", "It won't print", categoryId, requesterId, TicketPriority.Low, DateTime.UtcNow);

        Assert.Equal(TicketStatus.New, ticket.Status);
        Assert.Null(ticket.AssignedAgentId);
    }

    [Fact]
    public async Task ChangeStatusAsync_RejectsTransitionsOutsideTheStateMachine()
    {
        using var db = _factory.CreateContext();
        var (categoryId, requesterId) = await SeedReferenceDataAsync(db);
        var workflow = new TicketWorkflowService(db, new SlaCalculator(db), new TicketAssignmentService(db));
        var ticket = await workflow.CreateTicketAsync("t", "d", categoryId, requesterId, TicketPriority.Low, DateTime.UtcNow);

        await Assert.ThrowsAsync<TicketWorkflowException>(
            () => workflow.ChangeStatusAsync(ticket, TicketStatus.Resolved, DateTime.UtcNow));
    }

    [Fact]
    public async Task ChangeStatusAsync_SetsFirstRespondedAtOnce_AndDecrementsWorkloadOnResolve()
    {
        using var db = _factory.CreateContext();
        var (categoryId, requesterId) = await SeedReferenceDataAsync(db);
        var agent = new User { Email = "agent@test", DisplayName = "Agent", PasswordHash = "x", Role = UserRole.Agent };
        db.Users.Add(agent);
        await db.SaveChangesAsync();

        var workflow = new TicketWorkflowService(db, new SlaCalculator(db), new TicketAssignmentService(db));
        var ticket = await workflow.CreateTicketAsync("t", "d", categoryId, requesterId, TicketPriority.Medium, DateTime.UtcNow);
        Assert.Equal(1, (await db.Users.SingleAsync(u => u.Id == agent.Id)).OpenTicketCount);

        var firstResponse = DateTime.UtcNow;
        await workflow.ChangeStatusAsync(ticket, TicketStatus.InProgress, firstResponse);
        await workflow.ChangeStatusAsync(ticket, TicketStatus.OnHold, firstResponse.AddMinutes(5));
        await workflow.ChangeStatusAsync(ticket, TicketStatus.InProgress, firstResponse.AddMinutes(10));

        Assert.Equal(firstResponse, ticket.FirstRespondedAt);

        await workflow.ChangeStatusAsync(ticket, TicketStatus.Resolved, firstResponse.AddHours(1));

        Assert.Equal(TicketStatus.Resolved, ticket.Status);
        Assert.NotNull(ticket.ResolvedAt);
        Assert.Equal(0, (await db.Users.SingleAsync(u => u.Id == agent.Id)).OpenTicketCount);
    }

    [Fact]
    public async Task ReopenAsync_ThrowsWhenOutsideTheReopenWindow()
    {
        using var db = _factory.CreateContext();
        var (categoryId, requesterId) = await SeedReferenceDataAsync(db);
        var workflow = new TicketWorkflowService(db, new SlaCalculator(db), new TicketAssignmentService(db));
        var ticket = await workflow.CreateTicketAsync("t", "d", categoryId, requesterId, TicketPriority.Low, DateTime.UtcNow);

        ticket.Status = TicketStatus.Resolved;
        ticket.ResolvedAt = DateTime.UtcNow.AddDays(-30);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<TicketWorkflowException>(() => workflow.ReopenAsync(ticket, DateTime.UtcNow));
    }

    [Fact]
    public async Task ReopenAsync_ResetsResolutionFieldsAndBumpsReopenCount_WithinWindow()
    {
        using var db = _factory.CreateContext();
        var (categoryId, requesterId) = await SeedReferenceDataAsync(db);
        var agent = new User { Email = "agent@test", DisplayName = "Agent", PasswordHash = "x", Role = UserRole.Agent };
        db.Users.Add(agent);
        await db.SaveChangesAsync();

        var workflow = new TicketWorkflowService(db, new SlaCalculator(db), new TicketAssignmentService(db));
        var ticket = await workflow.CreateTicketAsync("t", "d", categoryId, requesterId, TicketPriority.Low, DateTime.UtcNow);
        await workflow.ChangeStatusAsync(ticket, TicketStatus.InProgress, DateTime.UtcNow);
        await workflow.ChangeStatusAsync(ticket, TicketStatus.Resolved, DateTime.UtcNow);
        ticket.ResolvedAt = DateTime.UtcNow.AddDays(-1);
        ticket.ResolutionBreached = true;
        await db.SaveChangesAsync();
        Assert.Equal(0, (await db.Users.SingleAsync(u => u.Id == agent.Id)).OpenTicketCount);

        await workflow.ReopenAsync(ticket, DateTime.UtcNow);

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Equal(1, ticket.ReopenCount);
        Assert.Null(ticket.ResolvedAt);
        Assert.False(ticket.ResolutionBreached);
        Assert.Equal(1, (await db.Users.SingleAsync(u => u.Id == agent.Id)).OpenTicketCount);
    }

    public void Dispose() => _factory.Dispose();
}
