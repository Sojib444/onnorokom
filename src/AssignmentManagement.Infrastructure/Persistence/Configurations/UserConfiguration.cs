using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="User"/>. The <see cref="EmailAddress"/> value object
/// is stored as a plain string column via a value converter; the unique index backs the
/// login lookup.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => new EmailAddress(value))
            .HasMaxLength(254)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(u => u.ClassId);
        builder.HasIndex(u => u.ClassId);

        builder.HasOne<Class>()
            .WithMany()
            .HasForeignKey(u => u.ClassId)
            .OnDelete(DeleteBehavior.SetNull);

        // Maps to PostgreSQL's xmin system column, which is updated automatically on
        // every write, giving optimistic concurrency protection for free.
        builder.Property<uint>("xmin").IsRowVersion();
    }
}
