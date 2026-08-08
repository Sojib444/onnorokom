using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Persistence.Repositories.Read;

/// <summary>
/// EF Core-backed implementation of <see cref="ITeacherAssignmentReadRepository"/>.
/// Queries always run untracked.
/// </summary>
public sealed class TeacherAssignmentReadRepository : ITeacherAssignmentReadRepository
{
    private readonly ReadDbContext _db;

    public TeacherAssignmentReadRepository(ReadDbContext db)
    {
        _db = db;
    }

    public Task<TeacherAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.TeacherAssignments.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<IReadOnlyList<TeacherAssignment>> GetByTeacherAsync(
        Guid teacherId,
        CancellationToken cancellationToken) =>
        _db.TeacherAssignments
            .AsNoTracking()
            .Where(t => t.TeacherId == teacherId)
            .OrderBy(t => t.ClassId)
            .ToListAsync(cancellationToken)
            .ContinueWith(result => (IReadOnlyList<TeacherAssignment>)result.Result, cancellationToken);

    public Task<IReadOnlyList<TeacherAssignment>> GetAllAsync(CancellationToken cancellationToken) =>
        _db.TeacherAssignments
            .AsNoTracking()
            .OrderBy(t => t.TeacherId)
            .ToListAsync(cancellationToken)
            .ContinueWith(result => (IReadOnlyList<TeacherAssignment>)result.Result, cancellationToken);

    public Task<bool> ExistsForTeacherAsync(
        Guid teacherId,
        Guid classId,
        Guid subjectId,
        CancellationToken cancellationToken) =>
        _db.TeacherAssignments.AnyAsync(
            t => t.TeacherId == teacherId && t.ClassId == classId && t.SubjectId == subjectId,
            cancellationToken);
}
