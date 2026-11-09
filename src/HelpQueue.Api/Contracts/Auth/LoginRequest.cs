using System.ComponentModel.DataAnnotations;

namespace HelpQueue.Api.Contracts.Auth;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
