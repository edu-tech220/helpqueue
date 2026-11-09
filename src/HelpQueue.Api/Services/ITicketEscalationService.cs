namespace HelpQueue.Api.Services;

public interface ITicketEscalationService
{
    /// <summary>Scans open tickets for SLA breaches, flags them and bumps priority. Returns how many tickets changed.</summary>
    Task<int> EscalateBreachedTicketsAsync(DateTime now, CancellationToken ct = default);
}
