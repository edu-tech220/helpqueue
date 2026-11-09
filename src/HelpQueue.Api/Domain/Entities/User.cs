using HelpQueue.Api.Domain.Enums;

namespace HelpQueue.Api.Domain.Entities;

public class User : IHasRowVersion
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    // Agent workload counter used by the auto-assignment algorithm.
    // Updated under optimistic concurrency so two concurrent assignments
    // can never silently overload the same agent.
    public int OpenTicketCount { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
