using System.ComponentModel.DataAnnotations;

namespace HelpQueue.Api.Contracts.Tickets;

public record AddCommentRequest([Required, MaxLength(4000)] string Body, bool IsInternal);
