using APIBank.Model;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Relacja Client -> Cards (jeden do wielu)
        modelBuilder.Entity<Client>()
            .HasMany(c => c.Cards)
            .WithOne(card => card.Owner)
            .HasForeignKey(card => card.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relacja Client -> PrimaryCard (jeden do jednego, opcjonalna)
        modelBuilder.Entity<Client>()
            .HasOne(c => c.PrimaryCard)
            .WithMany()
            .HasForeignKey(c => c.PrimaryCardNumber)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
