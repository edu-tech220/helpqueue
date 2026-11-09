namespace HelpQueue.Web.Models.Api;

public record TicketComment(int Id, string AuthorName, string Body, bool IsInternal, DateTime CreatedAt);
