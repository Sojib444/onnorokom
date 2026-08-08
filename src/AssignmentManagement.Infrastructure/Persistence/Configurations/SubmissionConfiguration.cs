using AssignmentManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Submission"/>. The one-submission-per-student rule is
/// enforced with a unique index on (AssignmentId, StudentId).
/// </summary>
public sealed class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("Submissions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Answer)
            .HasMaxLength(8000)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.Marks)
            .HasPrecision(10, 2);

        builder.Property(s => s.Feedback)
            .HasMaxLength(2000);

        builder.HasOne<Assignment>()
            .WithMany()
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.AssignmentId, s.StudentId })
            .IsUnique();

        builder.HasIndex(s => s.StudentId);

        // Maps to PostgreSQL's xmin system column, which is updated automatically on
        // every write, giving optimistic concurrency protection for free.
        builder.Property<uint>("xmin").IsRowVersion();
    }
}
