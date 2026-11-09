using HelpQueue.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HelpQueue.Api.Tests.Infrastructure;

/// <summary>
/// Keeps one open SQLite in-memory connection alive for the lifetime of a test
/// and hands out fresh, independently-tracked DbContext instances against it —
/// mirroring how separate HTTP requests each get their own scoped DbContext
/// over the same physical database.
/// </summary>
public sealed class SqliteContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public HelpQueueDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HelpQueueDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new HelpQueueDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
