using GameHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameHub.Infrastructure.Persistence.Configurations;

public class UserGameConfiguration : IEntityTypeConfiguration<UserGame>
{
    public void Configure(EntityTypeBuilder<UserGame> builder)
    {
        builder.ToTable("UserGames");

        builder.HasKey(ug => ug.Id);

        builder.Property(ug => ug.HoursPlayed);

        builder.Property(ug => ug.Status)
            .IsRequired();

        builder.Property(ug => ug.AddedAt)
            .IsRequired();

        builder.HasOne(ug => ug.User)
            .WithMany(u => u.UserGames)
            .HasForeignKey(ug => ug.UserId);

        builder.HasOne(ug => ug.Game)
            .WithMany(g => g.UserGames)
            .HasForeignKey(ug => ug.GameId);

        builder.HasIndex(ug => new { ug.UserId, ug.GameId })
            .IsUnique();
    }
}
