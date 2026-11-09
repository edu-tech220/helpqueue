using HelpQueue.Api.Data;
using HelpQueue.Api.Domain.Entities;
using HelpQueue.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpQueue.Api.Services;

/// <summary>
/// Picks the least-loaded active agent and atomically bumps their workload
/// counter. Two requests racing to assign different tickets to the same
/// "most available" agent must not both succeed without the counter
/// reflecting both — this is enforced with the agent's rowversion token and
/// a bounded retry loop instead of a pessimistic lock.
/// </summary>
public class TicketAssignmentService : ITicketAssignmentService
{
    private const int MaxAttempts = 5;

    private readonly HelpQueueDbContext _db;

    public TicketAssignmentService(HelpQueueDbContext db)
    {
        _db = db;
    }

    public async Task<User?> AssignLeastLoadedAgentAsync(Ticket ticket, CancellationToken ct = default)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var candidate = await _db.Users
                .Where(u => u.Role == UserRole.Agent && u.IsActive)
                .OrderBy(u => u.OpenTicketCount)
                .ThenBy(u => u.Id)
                .FirstOrDefaultAsync(ct);

            if (candidate is null)
            {
                return null;
            }

            candidate.OpenTicketCount++;
            ticket.AssignedAgentId = candidate.Id;
            ticket.Status = TicketStatus.Assigned;

            try
            {
                await _db.SaveChangesAsync(ct);
                return candidate;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < MaxAttempts)
            {
                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync(ct);
                }
            }
        }

        return null;
    }
}
