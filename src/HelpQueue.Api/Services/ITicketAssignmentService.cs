using HelpQueue.Api.Domain.Entities;

namespace HelpQueue.Api.Services;

public interface ITicketAssignmentService
{
    /// <summary>
    /// Assigns the ticket to the active agent with the fewest open tickets.
    /// Returns the assigned agent, or null if no agent is currently available
    /// (the ticket is left in <c>New</c> status for a manual/later assignment).
    /// </summary>
    Task<User?> AssignLeastLoadedAgentAsync(Ticket ticket, CancellationToken ct = default);
}
