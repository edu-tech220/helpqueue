using HelpQueue.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpQueue.Api.Data;

public class HelpQueueDbContext : DbContext
{
    public HelpQueueDbContext(DbContextOptions<HelpQueueDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<SlaPolicy>(entity =>
        {
            entity.HasKey(s => s.Priority);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.Property(t => t.RowVersion).IsRowVersion();

            entity.HasOne(t => t.Category)
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Requester)
                .WithMany()
                .HasForeignKey(t => t.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.AssignedAgent)
                .WithMany()
                .HasForeignKey(t => t.AssignedAgentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(t => new { t.Status, t.Priority });
            entity.HasIndex(t => t.AssignedAgentId);
            entity.HasIndex(t => t.RequesterId);
        });

        modelBuilder.Entity<TicketComment>(entity =>
        {
            entity.HasOne(c => c.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => c.TicketId);
        });
    }
}
