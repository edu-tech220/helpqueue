using HelpQueue.Api.Domain.Entities;
using HelpQueue.Api.Domain.Enums;
using HelpQueue.Api.Security;

namespace HelpQueue.Api.Data;

/// <summary>Demo reference data and accounts so the app is usable right after `dotnet run` with no manual setup.</summary>
public static class SeedData
{
    public const string DemoPassword = "Passw0rd!";

    public static void EnsureSeeded(HelpQueueDbContext db)
    {
        if (!db.Categories.Any())
        {
            db.Categories.AddRange(
                new Category { Name = "Hardware" },
                new Category { Name = "Software" },
                new Category { Name = "Network" },
                new Category { Name = "Account & Access" });
        }

        if (!db.SlaPolicies.Any())
        {
            db.SlaPolicies.AddRange(
                new SlaPolicy { Priority = TicketPriority.Critical, ResponseMinutes = 15, ResolutionMinutes = 4 * 60 },
                new SlaPolicy { Priority = TicketPriority.High, ResponseMinutes = 60, ResolutionMinutes = 8 * 60 },
                new SlaPolicy { Priority = TicketPriority.Medium, ResponseMinutes = 4 * 60, ResolutionMinutes = 3 * 24 * 60 },
                new SlaPolicy { Priority = TicketPriority.Low, ResponseMinutes = 8 * 60, ResolutionMinutes = 5 * 24 * 60 });
        }

        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new User
                {
                    Email = "admin@helpqueue.dev",
                    DisplayName = "Alex Admin",
                    PasswordHash = PasswordHasher.Hash(DemoPassword),
                    Role = UserRole.Admin
                },
                new User
                {
                    Email = "agent1@helpqueue.dev",
                    DisplayName = "Grace Agent",
                    PasswordHash = PasswordHasher.Hash(DemoPassword),
                    Role = UserRole.Agent
                },
                new User
                {
                    Email = "agent2@helpqueue.dev",
                    DisplayName = "Noah Agent",
                    PasswordHash = PasswordHasher.Hash(DemoPassword),
                    Role = UserRole.Agent
                },
                new User
                {
                    Email = "customer@helpqueue.dev",
                    DisplayName = "Casey Customer",
                    PasswordHash = PasswordHasher.Hash(DemoPassword),
                    Role = UserRole.Customer
                });
        }

        db.SaveChanges();
    }
}
