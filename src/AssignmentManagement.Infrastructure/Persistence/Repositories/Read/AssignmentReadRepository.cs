using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Persistence.Repositories.Read;

/// <summary>
/// EF Core-backed implementation of <see cref="IAssignmentReadRepository"/>. Queries
/// always run untracked; the role-based query methods keep the loaded graphs as small
/// as the views require.
/// </summary>
public sealed class AssignmentReadRepository : IAssignmentReadRepository
{
    private readonly ReadDbContext _db;

    public AssignmentReadRepository(ReadDbContext db)
    {
        _db = db;
    }

    public Task<Assignment?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Assignments
            .AsNoTracking()
            .Include(a => a.Class)
            .Include(a => a.Subject)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Assignments.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<IReadOnlyList<Assignment>> GetAllAsync(CancellationToken cancellationToken) =>
        _db.Assignments
            .AsNoTracking()
            .Include(a => a.Class)
            .Include(a => a.Subject)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken)
            .ContinueWith(result => (IReadOnlyList<Assignment>)result.Result, cancellationToken);

    public Task<IReadOnlyList<Assignment>> GetByTeacherAsync(
        Guid teacherId,
        CancellationToken cancellationToken) =>
        _db.Assignments
            .AsNoTracking()
            .Include(a => a.Class)
            .Include(a => a.Subject)
            .Where(a => a.TeacherId == teacherId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken)
            .ContinueWith(result => (IReadOnlyList<Assignment>)result.Result, cancellationToken);

    public Task<IReadOnlyList<Assignment>> GetByTeacherAndClassAsync(
        Guid teacherId,
        Guid classId,
        CancellationToken cancellationToken) =>
        _db.Assignments
            .AsNoTracking()
            .Include(a => a.Class)
            .Include(a => a.Subject)
            .Where(a => a.TeacherId == teacherId && a.ClassId == classId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken)
            .ContinueWith(result => (IReadOnlyList<Assignment>)result.Result, cancellationToken);

    public Task<IReadOnlyList<Assignment>> GetPublishedForClassAsync(
        Guid classId,
        CancellationToken cancellationToken) =>
        _db.Assignments
            .AsNoTracking()
            .Include(a => a.Class)
            .Include(a => a.Subject)
            .Where(a => a.ClassId == classId && a.Status == Domain.Enums.AssignmentStatus.Published)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken)
            .ContinueWith(result => (IReadOnlyList<Assignment>)result.Result, cancellationToken);

    public Task<bool> ExistsForTeacherAsync(
        Guid teacherId,
        CancellationToken cancellationToken) =>
        _db.Assignments.AnyAsync(a => a.TeacherId == teacherId, cancellationToken);

    public Task<bool> ExistsForClassAsync(
        Guid classId,
        CancellationToken cancellationToken) =>
        _db.Assignments.AnyAsync(a => a.ClassId == classId, cancellationToken);

    public Task<bool> ExistsForSubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken) =>
        _db.Assignments.AnyAsync(a => a.SubjectId == subjectId, cancellationToken);
}
