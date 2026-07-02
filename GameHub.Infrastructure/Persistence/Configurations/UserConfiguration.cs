using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameHub.Domain.Entities;


namespace GameHub.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        //Table
        builder.ToTable("Users");

        //Id
        builder.HasKey(u => u.Id);

        //UserName
        builder.Property(u => u.UserName)
            .IsRequired()
            .HasMaxLength(50);

        //Email
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100);

        //PasswordHash
        builder.Property(u => u.PasswordHash)
            .IsRequired();

        //CreatedAt
        builder.Property(u => u.CreatedAt)
            .IsRequired();
    }
}