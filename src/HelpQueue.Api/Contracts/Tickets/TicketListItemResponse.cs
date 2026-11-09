using HelpQueue.Api.Domain.Enums;

namespace HelpQueue.Api.Contracts.Tickets;

public record TicketListItemResponse(
    int Id,
    string Title,
    TicketStatus Status,
    TicketPriority Priority,
    string CategoryName,
    string? AssignedAgentName,
    DateTime ResponseDueAt,
    DateTime ResolutionDueAt,
    bool ResponseBreached,
    bool ResolutionBreached);
