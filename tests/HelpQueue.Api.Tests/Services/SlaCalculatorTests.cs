using HelpQueue.Api.Domain.Entities;
using HelpQueue.Api.Domain.Enums;
using HelpQueue.Api.Services;
using HelpQueue.Api.Tests.Infrastructure;
using Xunit;

namespace HelpQueue.Api.Tests.Services;

public class SlaCalculatorTests : IDisposable
{
    private readonly SqliteContextFactory _factory = new();

    [Fact]
    public async Task CalculateAsync_ReturnsDueDatesFromThePolicyTable()
    {
        using (var seed = _factory.CreateContext())
        {
            seed.SlaPolicies.Add(new SlaPolicy { Priority = TicketPriority.High, ResponseMinutes = 60, ResolutionMinutes = 480 });
            await seed.SaveChangesAsync();
        }

        using var db = _factory.CreateContext();
        var calculator = new SlaCalculator(db);
        var createdAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);

        var dueDates = await calculator.CalculateAsync(TicketPriority.High, createdAt);

        Assert.Equal(createdAt.AddMinutes(60), dueDates.ResponseDueAt);
        Assert.Equal(createdAt.AddMinutes(480), dueDates.ResolutionDueAt);
    }

    [Fact]
    public async Task CalculateAsync_ThrowsWhenNoPolicyIsConfigured()
    {
        using var db = _factory.CreateContext();
        var calculator = new SlaCalculator(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => calculator.CalculateAsync(TicketPriority.Critical, DateTime.UtcNow));
    }

    public void Dispose() => _factory.Dispose();
}
