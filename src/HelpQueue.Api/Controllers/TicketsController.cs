using System.Security.Claims;
using HelpQueue.Api.Contracts.Common;
using HelpQueue.Api.Contracts.Tickets;
using HelpQueue.Api.Data;
using HelpQueue.Api.Domain.Entities;
using HelpQueue.Api.Domain.Enums;
using HelpQueue.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpQueue.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly HelpQueueDbContext _db;
    private readonly ITicketWorkflowService _workflow;

    public TicketsController(HelpQueueDbContext db, ITicketWorkflowService workflow)
    {
        _db = db;
        _workflow = workflow;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private UserRole CurrentRole => Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!);

    [HttpPost]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<ActionResult<TicketResponse>> Create(CreateTicketRequest request, CancellationToken ct)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct);
        if (!categoryExists)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "The selected category does not exist.");
        }

        var ticket = await _workflow.CreateTicketAsync(
            request.Title, request.Description, request.CategoryId, CurrentUserId, request.Priority, DateTime.UtcNow, ct);

        var response = await LoadResponseAsync(ticket.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketListItemResponse>>> List(
        [FromQuery] TicketStatus? status,
        [FromQuery] TicketPriority? priority,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Tickets.AsNoTracking().AsQueryable();

        query = CurrentRole switch
        {
            UserRole.Customer => query.Where(t => t.RequesterId == CurrentUserId),
            UserRole.Agent => query.Where(t => t.AssignedAgentId == CurrentUserId),
            _ => query
        };

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status);
        }

        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.ResponseDueAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketListItemResponse(
                t.Id,
                t.Title,
                t.Status,
                t.Priority,
                t.Category!.Name,
                t.AssignedAgent == null ? null : t.AssignedAgent.DisplayName,
                t.ResponseDueAt,
                t.ResolutionDueAt,
                t.ResponseBreached,
                t.ResolutionBreached))
            .ToListAsync(ct);

        return Ok(new PagedResult<TicketListItemResponse>(items, page, pageSize, totalCount));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketResponse>> GetById(int id, CancellationToken ct)
    {
        var ticket = await FindAccessibleTicketAsync(id, ct);
        if (ticket is null)
        {
            return NotFound();
        }

        return Ok(await LoadResponseAsync(ticket.Id, ct));
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<ActionResult<TicketResponse>> ChangeStatus(int id, ChangeTicketStatusRequest request, CancellationToken ct)
    {
        var ticket = await FindAccessibleTicketAsync(id, ct);
        if (ticket is null)
        {
            return NotFound();
        }

        _db.Entry(ticket).Property(t => t.RowVersion).OriginalValue = request.RowVersion;

        try
        {
            await _workflow.ChangeStatusAsync(ticket, request.Status, DateTime.UtcNow, ct);
        }
        catch (TicketWorkflowException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "The ticket was modified by someone else. Reload and try again.");
        }

        return Ok(await LoadResponseAsync(ticket.Id, ct));
    }

    [HttpPost("{id:int}/reopen")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<ActionResult<TicketResponse>> Reopen(int id, CancellationToken ct)
    {
        var ticket = await FindAccessibleTicketAsync(id, ct);
        if (ticket is null)
        {
            return NotFound();
        }

        try
        {
            await _workflow.ReopenAsync(ticket, DateTime.UtcNow, ct);
        }
        catch (TicketWorkflowException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
        }

        return Ok(await LoadResponseAsync(ticket.Id, ct));
    }

    [HttpPost("{id:int}/reassign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TicketResponse>> Reassign(int id, ReassignTicketRequest request, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
        {
            return NotFound();
        }

        var agent = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.AgentId, ct);
        if (agent is null || agent.Role != UserRole.Agent || !agent.IsActive)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "The selected agent is not available.");
        }

        _db.Entry(ticket).Property(t => t.RowVersion).OriginalValue = request.RowVersion;

        if (ticket.AssignedAgentId is int previousAgentId && previousAgentId != agent.Id)
        {
            var previousAgent = await _db.Users.FirstOrDefaultAsync(u => u.Id == previousAgentId, ct);
            if (previousAgent is not null && previousAgent.OpenTicketCount > 0)
            {
                previousAgent.OpenTicketCount--;
            }
        }

        if (ticket.AssignedAgentId != agent.Id)
        {
            agent.OpenTicketCount++;
        }

        ticket.AssignedAgentId = agent.Id;
        if (ticket.Status == TicketStatus.New)
        {
            ticket.Status = TicketStatus.Assigned;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "The ticket was modified by someone else. Reload and try again.");
        }

        return Ok(await LoadResponseAsync(ticket.Id, ct));
    }

    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<TicketCommentResponse>> AddComment(int id, AddCommentRequest request, CancellationToken ct)
    {
        var ticket = await FindAccessibleTicketAsync(id, ct);
        if (ticket is null)
        {
            return NotFound();
        }

        if (request.IsInternal && CurrentRole == UserRole.Customer)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Customers cannot post internal notes.");
        }

        var comment = new TicketComment
        {
            TicketId = ticket.Id,
            AuthorId = CurrentUserId,
            Body = request.Body,
            IsInternal = request.IsInternal,
            CreatedAt = DateTime.UtcNow
        };

        _db.TicketComments.Add(comment);
        await _db.SaveChangesAsync(ct);

        var author = await _db.Users.FirstAsync(u => u.Id == CurrentUserId, ct);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id },
            new TicketCommentResponse(comment.Id, author.DisplayName, comment.Body, comment.IsInternal, comment.CreatedAt));
    }

    private async Task<Ticket?> FindAccessibleTicketAsync(int id, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
        {
            return null;
        }

        return CurrentRole switch
        {
            UserRole.Customer when ticket.RequesterId != CurrentUserId => null,
            UserRole.Agent when ticket.AssignedAgentId != CurrentUserId => null,
            _ => ticket
        };
    }

    private async Task<TicketResponse> LoadResponseAsync(int id, CancellationToken ct)
    {
        var ticket = await _db.Tickets
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Requester)
            .Include(t => t.AssignedAgent)
            .Include(t => t.Comments.OrderBy(c => c.CreatedAt))
            .ThenInclude(c => c.Author)
            .FirstAsync(t => t.Id == id, ct);

        var visibleComments = ticket.Comments
            .Where(c => !c.IsInternal || CurrentRole != UserRole.Customer)
            .Select(c => new TicketCommentResponse(c.Id, c.Author!.DisplayName, c.Body, c.IsInternal, c.CreatedAt))
            .ToList();

        return new TicketResponse(
            ticket.Id,
            ticket.Title,
            ticket.Description,
            ticket.Status,
            ticket.Priority,
            ticket.Category!.Name,
            ticket.Requester!.DisplayName,
            ticket.AssignedAgent?.DisplayName,
            ticket.CreatedAt,
            ticket.ResponseDueAt,
            ticket.ResolutionDueAt,
            ticket.FirstRespondedAt,
            ticket.ResolvedAt,
            ticket.ClosedAt,
            ticket.ResponseBreached,
            ticket.ResolutionBreached,
            ticket.ReopenCount,
            ticket.RowVersion,
            visibleComments);
    }
}
