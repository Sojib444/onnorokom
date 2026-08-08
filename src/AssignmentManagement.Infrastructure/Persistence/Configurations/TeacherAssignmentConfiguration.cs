using AssignmentManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="TeacherAssignment"/>. The combination of teacher,
/// class and subject is unique, which prevents duplicate allocations for the same pair.
/// </summary>
public sealed class TeacherAssignmentConfiguration : IEntityTypeConfiguration<TeacherAssignment>
{
    public void Configure(EntityTypeBuilder<TeacherAssignment> builder)
    {
        builder.ToTable("TeacherAssignments");
        builder.HasKey(t => t.Id);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Class>()
            .WithMany()
            .HasForeignKey(t => t.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(t => t.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.TeacherId, t.ClassId, t.SubjectId })
            .IsUnique();

        builder.HasIndex(t => t.TeacherId);
    }
}
