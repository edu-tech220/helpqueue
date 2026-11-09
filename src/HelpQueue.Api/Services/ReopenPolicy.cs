using HelpQueue.Api.Domain.Entities;
using HelpQueue.Api.Domain.Enums;

namespace HelpQueue.Api.Services;

/// <summary>Business rule for how long, and how many times, a settled ticket may come back to life.</summary>
public static class ReopenPolicy
{
    public static readonly TimeSpan ReopenWindow = TimeSpan.FromDays(7);
    public const int MaxReopens = 3;

    public static bool CanReopen(Ticket ticket, DateTime now)
    {
        if (ticket.Status is not (TicketStatus.Resolved or TicketStatus.Closed))
        {
            return false;
        }

        if (ticket.ReopenCount >= MaxReopens)
        {
            return false;
        }

        var settledAt = ticket.ClosedAt ?? ticket.ResolvedAt;
        return settledAt is not null && now <= settledAt.Value.Add(ReopenWindow);
    }
}
