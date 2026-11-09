namespace HelpQueue.Api.Contracts.Tickets;

public record TicketCommentResponse(int Id, string AuthorName, string Body, bool IsInternal, DateTime CreatedAt);
