using AssignmentManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Assignment"/>. The status enum is stored as an int
/// and indexed because the common queries filter on it.
/// </summary>
public sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(a => a.Deadline)
            .IsRequired();

        builder.Property(a => a.MaximumMarks)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Class)
            .WithMany()
            .HasForeignKey(a => a.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Subject)
            .WithMany()
            .HasForeignKey(a => a.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.ClassId, a.Status });
        builder.HasIndex(a => new { a.TeacherId, a.Status });

        // Maps to PostgreSQL's xmin system column, which is updated automatically on
        // every write, giving optimistic concurrency protection for free.
        builder.Property<uint>("xmin").IsRowVersion();
    }
}
