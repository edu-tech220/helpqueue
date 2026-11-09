using HelpQueue.Api.Domain.Entities;
using HelpQueue.Api.Domain.Enums;

namespace HelpQueue.Api.Services;

public interface ITicketWorkflowService
{
    Task<Ticket> CreateTicketAsync(
        string title, string description, int categoryId, int requesterId, TicketPriority priority,
        DateTime now, CancellationToken ct = default);

    /// <summary>Throws <see cref="TicketWorkflowException"/> if the transition is not allowed from the ticket's current status.</summary>
    Task ChangeStatusAsync(Ticket ticket, TicketStatus newStatus, DateTime now, CancellationToken ct = default);

    /// <summary>Throws <see cref="TicketWorkflowException"/> if the ticket is not eligible for reopening (see <see cref="ReopenPolicy"/>).</summary>
    Task ReopenAsync(Ticket ticket, DateTime now, CancellationToken ct = default);
}
