using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Persistence.Repositories.Write;

/// <summary>
/// EF Core-backed implementation of <see cref="ISubmissionWriteRepository"/>. Loaded
/// submissions (with their attachments) stay tracked by <see cref="WriteDbContext"/> so
/// grading, returning and answer edits persist with the unit of work.
/// </summary>
public sealed class SubmissionWriteRepository : ISubmissionWriteRepository
{
    private readonly WriteDbContext _db;

    public SubmissionWriteRepository(WriteDbContext db)
    {
        _db = db;
    }

    public Task<Submission?> GetByIdWithAttachmentsAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Submissions
            .Include(s => s.Attachments)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Add(Submission entity) => _db.Submissions.Add(entity);

    public void Update(Submission entity)
    {
        _db.Submissions.Update(entity);

        // A freshly created attachment carries a domain-generated Guid, so EF Core's
        // DetectChanges interprets its non-default key as "row already exists" and would
        // emit an UPDATE affecting 0 rows (optimistic concurrency failure). Track new
        // attachments as Added explicitly so they are inserted instead.
        foreach (var attachment in entity.Attachments)
        {
            var entry = _db.Entry(attachment);
            if (entry.State is EntityState.Detached or EntityState.Modified)
            {
                entry.State = EntityState.Added;
            }
        }
    }
}
