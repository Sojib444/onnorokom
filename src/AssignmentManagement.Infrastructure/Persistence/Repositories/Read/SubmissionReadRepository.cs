using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Persistence.Repositories.Read;

/// <summary>
/// EF Core-backed implementation of <see cref="ISubmissionReadRepository"/>. Queries
/// always run untracked.
/// </summary>
public sealed class SubmissionReadRepository : ISubmissionReadRepository
{
    private readonly ReadDbContext _db;

    public SubmissionReadRepository(ReadDbContext db)
    {
        _db = db;
    }

    public Task<Submission?> GetByIdWithAttachmentsAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Submissions
            .AsNoTracking()
            .Include(s => s.Attachments)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Submission?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Submissions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<IReadOnlyList<Submission>> GetByAssignmentAsync(
        Guid assignmentId,
        CancellationToken cancellationToken) =>
        _db.Submissions
            .AsNoTracking()
            .Include(s => s.Attachments)
            .Where(s => s.AssignmentId == assignmentId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(cancellationToken)
            .ContinueWith(result => (IReadOnlyList<Submission>)result.Result, cancellationToken);

    public Task<IReadOnlyList<Submission>> GetByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken) =>
        _db.Submissions
            .AsNoTracking()
            .Include(s => s.Attachments)
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(cancellationToken)
            .ContinueWith(result => (IReadOnlyList<Submission>)result.Result, cancellationToken);

    public Task<Submission?> GetByAssignmentAndStudentAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken cancellationToken) =>
        _db.Submissions
            .AsNoTracking()
            .Include(s => s.Attachments)
            .SingleOrDefaultAsync(
                s => s.AssignmentId == assignmentId && s.StudentId == studentId,
                cancellationToken);

    public Task<bool> ExistsForAssignmentAndStudentAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken cancellationToken) =>
        _db.Submissions.AnyAsync(
            s => s.AssignmentId == assignmentId && s.StudentId == studentId,
            cancellationToken);

    public Task<bool> ExistsForStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken) =>
        _db.Submissions.AnyAsync(s => s.StudentId == studentId, cancellationToken);
}
