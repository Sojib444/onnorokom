using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Persistence.Repositories.Write;

/// <summary>
/// EF Core-backed implementation of <see cref="ISubjectWriteRepository"/>. Loaded
/// aggregates stay tracked by <see cref="WriteDbContext"/> so edits persist with the
/// unit of work.
/// </summary>
public sealed class SubjectWriteRepository : ISubjectWriteRepository
{
    private readonly WriteDbContext _db;

    public SubjectWriteRepository(WriteDbContext db)
    {
        _db = db;
    }

    public Task<Subject?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Subjects.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Add(Subject entity) => _db.Subjects.Add(entity);

    public void Update(Subject entity) => _db.Subjects.Update(entity);

    public void Remove(Subject entity) => _db.Subjects.Remove(entity);
}
