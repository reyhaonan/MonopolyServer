using Microsoft.EntityFrameworkCore;
using MonopolyServer.Database.Entity;

namespace MonopolyServer.Database;

public class MonopolyDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    private IConfiguration _configuration;

    public MonopolyDbContext(IConfiguration configuration) {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        Console.WriteLine($"Connecting to {_configuration["Database:Connection"]}");
        optionsBuilder.UseNpgsql(_configuration["Database:Connection"]);
    }

    // protected override void OnModelCreating(ModelBuilder modelBuilder)
    //     {
    //         modelBuilder.Entity<User>().;
    //     }
}
