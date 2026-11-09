using HelpQueue.Api.Domain.Enums;

namespace HelpQueue.Api.Services;

/// <summary>
/// Allowed plain status transitions. Reopening a Resolved/Closed ticket is a
/// separate, explicit operation (see <see cref="ReopenPolicy"/>) and is not
/// reachable through this map on purpose.
/// </summary>
public static class TicketTransitions
{
    private static readonly Dictionary<TicketStatus, TicketStatus[]> Allowed = new()
    {
        [TicketStatus.New] = new[] { TicketStatus.Assigned },
        [TicketStatus.Assigned] = new[] { TicketStatus.InProgress, TicketStatus.OnHold },
        [TicketStatus.InProgress] = new[] { TicketStatus.OnHold, TicketStatus.Resolved },
        [TicketStatus.OnHold] = new[] { TicketStatus.InProgress },
        [TicketStatus.Resolved] = new[] { TicketStatus.Closed },
        [TicketStatus.Closed] = Array.Empty<TicketStatus>()
    };

    public static bool CanTransition(TicketStatus from, TicketStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
}
