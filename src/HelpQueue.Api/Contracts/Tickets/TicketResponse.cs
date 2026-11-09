using HelpQueue.Api.Domain.Enums;

namespace HelpQueue.Api.Contracts.Tickets;

public record TicketResponse(
    int Id,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    string CategoryName,
    string RequesterName,
    string? AssignedAgentName,
    DateTime CreatedAt,
    DateTime ResponseDueAt,
    DateTime ResolutionDueAt,
    DateTime? FirstRespondedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    bool ResponseBreached,
    bool ResolutionBreached,
    int ReopenCount,
    byte[] RowVersion,
    IReadOnlyList<TicketCommentResponse> Comments);
