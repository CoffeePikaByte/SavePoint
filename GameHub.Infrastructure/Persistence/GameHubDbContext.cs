using GameHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Infrastructure.Persistence;

public class GameHubDbContext : DbContext
{
    public GameHubDbContext(DbContextOptions<GameHubDbContext> options) 
        : base(options)
    {
        
    }

    public DbSet<Game> Games { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserGame> UserGames { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameHubDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

}