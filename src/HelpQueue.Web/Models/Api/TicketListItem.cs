namespace HelpQueue.Web.Models.Api;

public record TicketListItem(
    int Id,
    string Title,
    string Status,
    string Priority,
    string CategoryName,
    string? AssignedAgentName,
    DateTime ResponseDueAt,
    DateTime ResolutionDueAt,
    bool ResponseBreached,
    bool ResolutionBreached);
