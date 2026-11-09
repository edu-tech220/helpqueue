using HelpQueue.Api.Domain.Enums;

namespace HelpQueue.Api.Domain.Entities;

public class Ticket
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.New;
    public TicketPriority Priority { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int RequesterId { get; set; }
    public User? Requester { get; set; }

    public int? AssignedAgentId { get; set; }
    public User? AssignedAgent { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ResponseDueAt { get; set; }
    public DateTime ResolutionDueAt { get; set; }

    public DateTime? FirstRespondedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public bool ResponseBreached { get; set; }
    public bool ResolutionBreached { get; set; }

    public int ReopenCount { get; set; }

    public List<TicketComment> Comments { get; set; } = new();

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
