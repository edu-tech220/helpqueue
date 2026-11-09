namespace HelpQueue.Api.Contracts.Tickets;

public record ReassignTicketRequest(int AgentId, byte[] RowVersion);
