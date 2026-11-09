namespace HelpQueue.Api.Domain.Entities;

public class TicketComment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public int AuthorId { get; set; }
    public User? Author { get; set; }

    public required string Body { get; set; }

    /// <summary>Internal notes are only visible to agents and admins, never to the requester.</summary>
    public bool IsInternal { get; set; }

    public DateTime CreatedAt { get; set; }
}
