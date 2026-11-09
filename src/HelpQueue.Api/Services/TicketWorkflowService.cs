using HelpQueue.Api.Data;
using HelpQueue.Api.Domain.Entities;
using HelpQueue.Api.Domain.Enums;

namespace HelpQueue.Api.Services;

public class TicketWorkflowService : ITicketWorkflowService
{
    private readonly HelpQueueDbContext _db;
    private readonly ISlaCalculator _slaCalculator;
    private readonly ITicketAssignmentService _assignmentService;

    public TicketWorkflowService(HelpQueueDbContext db, ISlaCalculator slaCalculator, ITicketAssignmentService assignmentService)
    {
        _db = db;
        _slaCalculator = slaCalculator;
        _assignmentService = assignmentService;
    }

    public async Task<Ticket> CreateTicketAsync(
        string title, string description, int categoryId, int requesterId, TicketPriority priority,
        DateTime now, CancellationToken ct = default)
    {
        var sla = await _slaCalculator.CalculateAsync(priority, now, ct);

        var ticket = new Ticket
        {
            Title = title,
            Description = description,
            CategoryId = categoryId,
            RequesterId = requesterId,
            Priority = priority,
            Status = TicketStatus.New,
            CreatedAt = now,
            ResponseDueAt = sla.ResponseDueAt,
            ResolutionDueAt = sla.ResolutionDueAt
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);

        // Best-effort: if no agent is free the ticket simply stays New for manual assignment later.
        await _assignmentService.AssignLeastLoadedAgentAsync(ticket, ct);

        return ticket;
    }

    public async Task ChangeStatusAsync(Ticket ticket, TicketStatus newStatus, DateTime now, CancellationToken ct = default)
    {
        if (!TicketTransitions.CanTransition(ticket.Status, newStatus))
        {
            throw new TicketWorkflowException($"Cannot move ticket from '{ticket.Status}' to '{newStatus}'.");
        }

        if (newStatus == TicketStatus.InProgress)
        {
            ticket.FirstRespondedAt ??= now;
        }

        if (newStatus == TicketStatus.Resolved)
        {
            ticket.ResolvedAt = now;
            await DecrementAgentWorkloadAsync(ticket.AssignedAgentId, ct);
        }

        if (newStatus == TicketStatus.Closed)
        {
            ticket.ClosedAt = now;
        }

        ticket.Status = newStatus;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReopenAsync(Ticket ticket, DateTime now, CancellationToken ct = default)
    {
        if (!ReopenPolicy.CanReopen(ticket, now))
        {
            throw new TicketWorkflowException("Ticket is outside the reopen window or has been reopened too many times.");
        }

        var sla = await _slaCalculator.CalculateAsync(ticket.Priority, now, ct);

        ticket.Status = TicketStatus.InProgress;
        ticket.ReopenCount++;
        ticket.ResolvedAt = null;
        ticket.ClosedAt = null;
        ticket.ResolutionBreached = false;
        ticket.ResolutionDueAt = sla.ResolutionDueAt;

        await IncrementAgentWorkloadAsync(ticket.AssignedAgentId, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task DecrementAgentWorkloadAsync(int? agentId, CancellationToken ct)
    {
        if (agentId is null)
        {
            return;
        }

        var agent = await _db.Users.FindAsync(new object[] { agentId.Value }, ct);
        if (agent is not null && agent.OpenTicketCount > 0)
        {
            agent.OpenTicketCount--;
        }
    }

    private async Task IncrementAgentWorkloadAsync(int? agentId, CancellationToken ct)
    {
        if (agentId is null)
        {
            return;
        }

        var agent = await _db.Users.FindAsync(new object[] { agentId.Value }, ct);
        if (agent is not null)
        {
            agent.OpenTicketCount++;
        }
    }
}
