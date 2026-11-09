namespace HelpQueue.Api.Contracts.Auth;

public record LoginResponse(string Token, DateTime ExpiresAtUtc, int UserId, string DisplayName, string Role);
