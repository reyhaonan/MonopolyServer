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
        var hostname = _configuration["Database:Host"] ?? _configuration["POSTGRES_HOST"] ?? throw new Exception("Database:Host is missing from configuration");
        var port = _configuration["Database:Port"] ?? _configuration["POSTGRES_PORT"] ??throw new Exception("Database:Port is missing from configuration");
        var username = _configuration["Database:Username"] ?? _configuration["POSTGRES_USERNAME"] ?? throw new Exception("Database:Username is missing from configuration");
        var password = _configuration["Database:Password"] ?? _configuration["POSTGRES_PASSWORD"] ?? throw new Exception("Database:Password is missing from configuration");
        var databaseName = _configuration["Database:DatabaseName"] ?? _configuration["POSTGRES_DATABASE"] ?? throw new Exception("Database:DatabaseName is missing from configuration");
        string connectionString = $"Host={hostname};Username={username};Password={password};Database={databaseName};Port={port}";
        
        optionsBuilder.UseNpgsql(connectionString);
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
