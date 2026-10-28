using Ledgerly.Core.Domain;
using Ledgerly.Core.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Core.Data;

public sealed class LedgerlyDbContext(DbContextOptions<LedgerlyDbContext> options) : DbContext(options)
{
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Import> Imports => Set<Import>();
    public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Portfolio>(e =>
        {
            e.Property(p => p.OwnerId).HasMaxLength(128);
            e.Property(p => p.Name).HasMaxLength(100);
            e.Property(p => p.BaseCurrency).HasMaxLength(3).IsFixedLength();
            e.Property(p => p.RowVersion).IsRowVersion();
            e.HasIndex(p => p.OwnerId);
            e.HasMany(p => p.Transactions).WithOne().HasForeignKey(t => t.PortfolioId);
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.Property(t => t.Symbol).HasMaxLength(12);
            e.Property(t => t.Type).HasConversion<string>().HasMaxLength(4);
            e.Property(t => t.Quantity).HasPrecision(18, 6);
            e.Property(t => t.Price).HasPrecision(18, 6);
            e.Property(t => t.ExternalRef).HasMaxLength(64);
            e.HasIndex(t => new { t.PortfolioId, t.ExternalRef })
                .IsUnique()
                .HasFilter("[ExternalRef] IS NOT NULL");
        });

        modelBuilder.Entity<Import>(e =>
        {
            e.Property(i => i.OwnerId).HasMaxLength(128);
            e.Property(i => i.FileName).HasMaxLength(260);
            e.Property(i => i.Status).HasConversion<string>().HasMaxLength(16);
            e.Property(i => i.Error).HasMaxLength(2000);
        });

        modelBuilder.Entity<PriceAlert>(e =>
        {
            e.Property(a => a.OwnerId).HasMaxLength(128);
            e.Property(a => a.Symbol).HasMaxLength(12);
            e.Property(a => a.TargetPrice).HasPrecision(18, 6);
            e.Property(a => a.Direction).HasConversion<string>().HasMaxLength(5);
            e.HasIndex(a => new { a.IsActive, a.Symbol });
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.Property(n => n.OwnerId).HasMaxLength(128);
            e.Property(n => n.Message).HasMaxLength(500);
            e.HasIndex(n => n.OwnerId);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.Property(m => m.Type).HasMaxLength(100);
            e.Property(m => m.Destination).HasMaxLength(100);
            e.Property(m => m.LastError).HasMaxLength(2000);
            e.HasIndex(m => m.ProcessedAt);
        });

        modelBuilder.Entity<ProcessedMessage>(e =>
        {
            e.HasKey(m => m.MessageId);
            e.Property(m => m.MessageId).HasMaxLength(64);
            e.Property(m => m.Handler).HasMaxLength(100);
        });
    }
}
