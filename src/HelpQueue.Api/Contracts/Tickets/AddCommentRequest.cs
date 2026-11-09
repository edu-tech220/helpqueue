using System.ComponentModel.DataAnnotations;

namespace HelpQueue.Api.Contracts.Tickets;

public record AddCommentRequest([property: Required, MaxLength(4000)] string Body, bool IsInternal);
