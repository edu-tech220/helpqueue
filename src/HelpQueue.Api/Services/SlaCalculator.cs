using HelpQueue.Api.Data;
using HelpQueue.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpQueue.Api.Services;

public class SlaCalculator : ISlaCalculator
{
    private readonly HelpQueueDbContext _db;

    public SlaCalculator(HelpQueueDbContext db)
    {
        _db = db;
    }

    public async Task<SlaDueDates> CalculateAsync(TicketPriority priority, DateTime createdAt, CancellationToken ct = default)
    {
        var policy = await _db.SlaPolicies.FindAsync(new object[] { priority }, ct)
            ?? throw new InvalidOperationException($"No SLA policy configured for priority '{priority}'.");

        return new SlaDueDates(
            createdAt.AddMinutes(policy.ResponseMinutes),
            createdAt.AddMinutes(policy.ResolutionMinutes));
    }
}
