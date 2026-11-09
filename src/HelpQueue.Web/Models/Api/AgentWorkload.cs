namespace HelpQueue.Web.Models.Api;

public record AgentWorkload(int Id, string DisplayName, string Email, bool IsActive, int OpenTicketCount);
