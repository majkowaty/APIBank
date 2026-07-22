using APIBank.Model;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client> Clients { get; set; } = null!;
    public DbSet<Card> Cards { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Client.Cards -> Card.Owner (jeden do wielu)
        modelBuilder.Entity<Client>()
            .HasMany(c => c.Cards)
            .WithOne(card => card.Owner)
            .HasForeignKey(card => card.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Client.PrimaryCard -> Card (opcjonalne, FK po stronie Client)
        modelBuilder.Entity<Client>()
            .HasOne(c => c.PrimaryCard)
            .WithMany()
            .HasForeignKey(c => c.PrimaryCardNumber)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // User -> Client (1:1, FK po stronie User)
        modelBuilder.Entity<User>()
            .HasOne(u => u.Client)
            .WithOne(c => c.User)
            .HasForeignKey<User>(u => u.ClientAccountId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
    }
}
