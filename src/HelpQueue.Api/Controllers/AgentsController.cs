using HelpQueue.Api.Contracts.Agents;
using HelpQueue.Api.Data;
using HelpQueue.Api.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpQueue.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/agents")]
public class AgentsController : ControllerBase
{
    private readonly HelpQueueDbContext _db;

    public AgentsController(HelpQueueDbContext db)
    {
        _db = db;
    }

    [HttpGet("workload")]
    public async Task<ActionResult<IReadOnlyList<AgentWorkloadResponse>>> Workload(CancellationToken ct)
    {
        var agents = await _db.Users
            .Where(u => u.Role == UserRole.Agent)
            .OrderBy(u => u.OpenTicketCount)
            .Select(u => new AgentWorkloadResponse(u.Id, u.DisplayName, u.Email, u.IsActive, u.OpenTicketCount))
            .ToListAsync(ct);

        return Ok(agents);
    }
}
