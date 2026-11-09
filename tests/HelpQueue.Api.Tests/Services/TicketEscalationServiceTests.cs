using HelpQueue.Api.Domain.Entities;
using HelpQueue.Api.Domain.Enums;
using HelpQueue.Api.Services;
using HelpQueue.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HelpQueue.Api.Tests.Services;

public class TicketEscalationServiceTests : IDisposable
{
    private readonly SqliteContextFactory _factory = new();

    [Fact]
    public async Task EscalateBreachedTicketsAsync_FlagsResponseBreach_AndBumpsPriority()
    {
        var now = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        int ticketId;
        using (var seed = _factory.CreateContext())
        {
            var ticket = await SeedTicketAsync(seed, TicketPriority.Medium, responseDueAt: now.AddMinutes(-5), resolutionDueAt: now.AddDays(2));
            await seed.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        using var db = _factory.CreateContext();
        var service = new TicketEscalationService(db);

        var changed = await service.EscalateBreachedTicketsAsync(now);

        Assert.Equal(1, changed);
        var ticketAfter = await db.Tickets.SingleAsync(t => t.Id == ticketId);
        Assert.True(ticketAfter.ResponseBreached);
        Assert.Equal(TicketPriority.High, ticketAfter.Priority);
        Assert.False(ticketAfter.ResolutionBreached);
    }

    [Fact]
    public async Task EscalateBreachedTicketsAsync_FlagsResolutionBreach_AndForcesCriticalPriority()
    {
        var now = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        int ticketId;
        using (var seed = _factory.CreateContext())
        {
            var ticket = await SeedTicketAsync(seed, TicketPriority.Low, responseDueAt: now.AddHours(-1), resolutionDueAt: now.AddMinutes(-1));
            ticket.FirstRespondedAt = now.AddHours(-2);
            await seed.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        using var db = _factory.CreateContext();
        var service = new TicketEscalationService(db);

        await service.EscalateBreachedTicketsAsync(now);

        var ticketAfter = await db.Tickets.SingleAsync(t => t.Id == ticketId);
        Assert.True(ticketAfter.ResolutionBreached);
        Assert.Equal(TicketPriority.Critical, ticketAfter.Priority);
    }

    [Fact]
    public async Task EscalateBreachedTicketsAsync_IgnoresResolvedAndClosedTickets()
    {
        var now = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        using (var seed = _factory.CreateContext())
        {
            var ticket = await SeedTicketAsync(seed, TicketPriority.Medium, responseDueAt: now.AddDays(-1), resolutionDueAt: now.AddDays(-1));
            ticket.Status = TicketStatus.Resolved;
            ticket.ResolvedAt = now.AddHours(-1);
            await seed.SaveChangesAsync();
        }

        using var db = _factory.CreateContext();
        var service = new TicketEscalationService(db);

        var changed = await service.EscalateBreachedTicketsAsync(now);

        Assert.Equal(0, changed);
    }

    [Fact]
    public async Task EscalateBreachedTicketsAsync_IsIdempotent_OnASecondSweep()
    {
        var now = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        using (var seed = _factory.CreateContext())
        {
            await SeedTicketAsync(seed, TicketPriority.Low, responseDueAt: now.AddMinutes(-5), resolutionDueAt: now.AddDays(2));
            await seed.SaveChangesAsync();
        }

        using var db = _factory.CreateContext();
        var service = new TicketEscalationService(db);

        var firstSweep = await service.EscalateBreachedTicketsAsync(now);
        var secondSweep = await service.EscalateBreachedTicketsAsync(now.AddMinutes(1));

        Assert.Equal(1, firstSweep);
        Assert.Equal(0, secondSweep);
    }

    private static async Task<Ticket> SeedTicketAsync(
        HelpQueue.Api.Data.HelpQueueDbContext db, TicketPriority priority, DateTime responseDueAt, DateTime resolutionDueAt)
    {
        var category = new Category { Name = "General" };
        var requester = new User { Email = $"c{Guid.NewGuid():N}@test", DisplayName = "Customer", PasswordHash = "x", Role = UserRole.Customer };
        db.Categories.Add(category);
        db.Users.Add(requester);
        await db.SaveChangesAsync();

        var ticket = new Ticket
        {
            Title = "Needs help",
            Description = "Something is broken",
            Priority = priority,
            Status = TicketStatus.Assigned,
            CategoryId = category.Id,
            RequesterId = requester.Id,
            CreatedAt = responseDueAt.AddHours(-1),
            ResponseDueAt = responseDueAt,
            ResolutionDueAt = resolutionDueAt
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        return ticket;
    }

    public void Dispose() => _factory.Dispose();
}
