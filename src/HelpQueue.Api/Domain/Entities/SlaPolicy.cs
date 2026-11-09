using HelpQueue.Api.Domain.Enums;

namespace HelpQueue.Api.Domain.Entities;

/// <summary>Response/resolution targets per priority, seeded once and read at ticket creation time.</summary>
public class SlaPolicy
{
    public TicketPriority Priority { get; set; }
    public int ResponseMinutes { get; set; }
    public int ResolutionMinutes { get; set; }
}
