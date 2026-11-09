using HelpQueue.Api.Domain.Enums;
using HelpQueue.Api.Services;
using Xunit;

namespace HelpQueue.Api.Tests.Services;

public class TicketTransitionsTests
{
    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.Assigned)]
    [InlineData(TicketStatus.Assigned, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Assigned, TicketStatus.OnHold)]
    [InlineData(TicketStatus.InProgress, TicketStatus.OnHold)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved)]
    [InlineData(TicketStatus.OnHold, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed)]
    public void CanTransition_AllowsExpectedPaths(TicketStatus from, TicketStatus to)
    {
        Assert.True(TicketTransitions.CanTransition(from, to));
    }

    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Resolved, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Closed, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Closed, TicketStatus.Assigned)]
    [InlineData(TicketStatus.InProgress, TicketStatus.New)]
    [InlineData(TicketStatus.OnHold, TicketStatus.Resolved)]
    public void CanTransition_RejectsPathsOutsideTheMap(TicketStatus from, TicketStatus to)
    {
        Assert.False(TicketTransitions.CanTransition(from, to));
    }
}
