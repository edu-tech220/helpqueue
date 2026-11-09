using HelpQueue.Api.Contracts.Auth;
using HelpQueue.Api.Data;
using HelpQueue.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace HelpQueue.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly HelpQueueDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthController(HelpQueueDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user is null || !user.IsActive || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password.");
        }

        var (token, expiresAt) = _tokenService.GenerateToken(user);
        return Ok(new LoginResponse(token, expiresAt, user.Id, user.DisplayName, user.Role.ToString()));
    }
}
