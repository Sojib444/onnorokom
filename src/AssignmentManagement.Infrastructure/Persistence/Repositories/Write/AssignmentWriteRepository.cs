using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Persistence.Repositories.Write;

/// <summary>
/// EF Core-backed implementation of <see cref="IAssignmentWriteRepository"/>. Loaded
/// aggregates stay tracked by <see cref="WriteDbContext"/> so edits persist with the
/// unit of work.
/// </summary>
public sealed class AssignmentWriteRepository : IAssignmentWriteRepository
{
    private readonly WriteDbContext _db;

    public AssignmentWriteRepository(WriteDbContext db)
    {
        _db = db;
    }

    public Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Assignments.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

    public void Add(Assignment entity) => _db.Assignments.Add(entity);

    public void Update(Assignment entity) => _db.Assignments.Update(entity);

    public void Remove(Assignment entity) => _db.Assignments.Remove(entity);
}
