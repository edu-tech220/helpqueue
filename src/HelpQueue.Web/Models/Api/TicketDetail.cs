namespace HelpQueue.Web.Models.Api;

public record TicketDetail(
    int Id,
    string Title,
    string Description,
    string Status,
    string Priority,
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
    List<TicketComment> Comments);
