using Microsoft.EntityFrameworkCore;
using MonopolyServer.Database.Entities;
using MonopolyServer.Database.Enums;

namespace MonopolyServer.Database;

public class MonopolyDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserOAuth> UserOAuth { get; set; }
    private IConfiguration _configuration;

    public MonopolyDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        Console.WriteLine($"Connecting to {_configuration["Database:Connection"]}");
        optionsBuilder.UseNpgsql(_configuration["Database:Connection"]);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.Property(e => e.Username).HasColumnType("varchar(255)");
        });
        modelBuilder.Entity<User>()
            .HasMany(e => e.OAuth)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId);


        modelBuilder.Entity<UserOAuth>(user =>
        {
            user.Property(e => e.ProviderName).HasConversion(v => v.ToString(), v => (ProviderName)Enum.Parse(typeof(ProviderName), v));
        });

        modelBuilder.Entity<UserOAuth>().HasIndex(e => new { e.ProviderName, e.OAuthID }).IsUnique();
    }

}
