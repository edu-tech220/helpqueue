namespace HelpQueue.Api.Services;

/// <summary>Raised when a requested ticket operation violates a business rule (invalid transition, reopen window expired, etc).</summary>
public class TicketWorkflowException : Exception
{
    public TicketWorkflowException(string message) : base(message)
    {
    }
}
