using HelpQueue.Api.Data;
using HelpQueue.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpQueue.Api.Services;

public class TicketEscalationService : ITicketEscalationService
{
    private readonly HelpQueueDbContext _db;

    public TicketEscalationService(HelpQueueDbContext db)
    {
        _db = db;
    }

    public async Task<int> EscalateBreachedTicketsAsync(DateTime now, CancellationToken ct = default)
    {
        var openTickets = await _db.Tickets
            .Where(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed)
            .Where(t =>
                (!t.ResponseBreached && t.FirstRespondedAt == null && t.ResponseDueAt < now) ||
                (!t.ResolutionBreached && t.ResolutionDueAt < now))
            .ToListAsync(ct);

        foreach (var ticket in openTickets)
        {
            if (!ticket.ResponseBreached && ticket.FirstRespondedAt == null && ticket.ResponseDueAt < now)
            {
                ticket.ResponseBreached = true;
                ticket.Priority = BumpPriority(ticket.Priority);
            }

            if (!ticket.ResolutionBreached && ticket.ResolutionDueAt < now)
            {
                ticket.ResolutionBreached = true;
                ticket.Priority = TicketPriority.Critical;
            }
        }

        if (openTickets.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        return openTickets.Count;
    }

    private static TicketPriority BumpPriority(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => TicketPriority.Medium,
        TicketPriority.Medium => TicketPriority.High,
        TicketPriority.High => TicketPriority.Critical,
        _ => TicketPriority.Critical
    };
}
