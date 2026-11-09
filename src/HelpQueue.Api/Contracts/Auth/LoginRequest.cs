using System.ComponentModel.DataAnnotations;

namespace HelpQueue.Api.Contracts.Auth;

public record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);
