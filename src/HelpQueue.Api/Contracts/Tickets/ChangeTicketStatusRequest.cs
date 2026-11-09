using HelpQueue.Api.Domain.Enums;

namespace HelpQueue.Api.Contracts.Tickets;

public record ChangeTicketStatusRequest(TicketStatus Status, byte[] RowVersion);
