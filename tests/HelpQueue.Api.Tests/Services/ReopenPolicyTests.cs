using HelpQueue.Api.Domain.Entities;
using HelpQueue.Api.Domain.Enums;
using HelpQueue.Api.Services;
using Xunit;

namespace HelpQueue.Api.Tests.Services;

public class ReopenPolicyTests
{
    private static Ticket ResolvedTicket(DateTime resolvedAt, int reopenCount = 0) => new()
    {
        Title = "t",
        Description = "d",
        Status = TicketStatus.Resolved,
        ResolvedAt = resolvedAt,
        ReopenCount = reopenCount
    };

    [Fact]
    public void CanReopen_ReturnsTrue_WithinWindow()
    {
        var now = new DateTime(2026, 1, 10);
        var ticket = ResolvedTicket(now.AddDays(-2));

        Assert.True(ReopenPolicy.CanReopen(ticket, now));
    }

    [Fact]
    public void CanReopen_ReturnsFalse_AfterWindowExpires()
    {
        var now = new DateTime(2026, 1, 10);
        var ticket = ResolvedTicket(now.AddDays(-8));

        Assert.False(ReopenPolicy.CanReopen(ticket, now));
    }

    [Fact]
    public void CanReopen_ReturnsFalse_WhenMaxReopensReached()
    {
        var now = new DateTime(2026, 1, 10);
        var ticket = ResolvedTicket(now.AddDays(-1), reopenCount: ReopenPolicy.MaxReopens);

        Assert.False(ReopenPolicy.CanReopen(ticket, now));
    }

    [Fact]
    public void CanReopen_ReturnsFalse_WhenTicketIsStillOpen()
    {
        var now = new DateTime(2026, 1, 10);
        var ticket = ResolvedTicket(now.AddDays(-1));
        ticket.Status = TicketStatus.InProgress;

        Assert.False(ReopenPolicy.CanReopen(ticket, now));
    }
}
