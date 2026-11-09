using Microsoft.Extensions.Options;

namespace HelpQueue.Api.Services;

public class EscalationOptions
{
    public int PollingSeconds { get; set; } = 60;
}

/// <summary>Periodically scans for SLA breaches so a ticket does not need a human to notice it blew its deadline.</summary>
public class TicketEscalationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketEscalationBackgroundService> _logger;
    private readonly TimeSpan _interval;

    public TicketEscalationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<EscalationOptions> options,
        ILogger<TicketEscalationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(Math.Max(options.Value.PollingSeconds, 5));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var escalationService = scope.ServiceProvider.GetRequiredService<ITicketEscalationService>();
                var escalated = await escalationService.EscalateBreachedTicketsAsync(DateTime.UtcNow, stoppingToken);

                if (escalated > 0)
                {
                    _logger.LogInformation("Escalated {Count} ticket(s) for SLA breach", escalated);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Ticket escalation sweep failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
