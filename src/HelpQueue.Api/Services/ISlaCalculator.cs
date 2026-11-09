using HelpQueue.Api.Domain.Enums;

namespace HelpQueue.Api.Services;

public interface ISlaCalculator
{
    Task<SlaDueDates> CalculateAsync(TicketPriority priority, DateTime createdAt, CancellationToken ct = default);
}

public record SlaDueDates(DateTime ResponseDueAt, DateTime ResolutionDueAt);
