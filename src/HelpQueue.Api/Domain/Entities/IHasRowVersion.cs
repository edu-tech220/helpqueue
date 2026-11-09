namespace HelpQueue.Api.Domain.Entities;

/// <summary>
/// Marks an entity with an app-managed optimistic-concurrency token. Unlike a
/// SQL Server `rowversion` column, this is stamped by HelpQueueDbContext on
/// every insert/update, so it behaves identically on SQL Server and SQLite.
/// </summary>
public interface IHasRowVersion
{
    byte[] RowVersion { get; set; }
}
