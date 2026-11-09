namespace HelpQueue.Api.Contracts.Agents;

public record AgentWorkloadResponse(int Id, string DisplayName, string Email, bool IsActive, int OpenTicketCount);
