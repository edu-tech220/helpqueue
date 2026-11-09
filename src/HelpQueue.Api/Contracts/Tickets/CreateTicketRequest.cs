using System.ComponentModel.DataAnnotations;
using HelpQueue.Api.Domain.Enums;

namespace HelpQueue.Api.Contracts.Tickets;

public record CreateTicketRequest(
    [property: Required, MaxLength(150)] string Title,
    [property: Required, MaxLength(4000)] string Description,
    int CategoryId,
    TicketPriority Priority);
