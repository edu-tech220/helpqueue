using HelpQueue.Api.Domain.Entities;

namespace HelpQueue.Api.Security;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
}
