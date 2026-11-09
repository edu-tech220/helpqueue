namespace HelpQueue.Web.Models.Api;

public record LoginResult(string Token, DateTime ExpiresAtUtc, int UserId, string DisplayName, string Role);
