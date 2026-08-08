using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Persistence.Repositories.Write;

/// <summary>
/// EF Core-backed implementation of <see cref="IClassWriteRepository"/>. Loaded
/// aggregates stay tracked by <see cref="WriteDbContext"/> so edits persist with the
/// unit of work.
/// </summary>
public sealed class ClassWriteRepository : IClassWriteRepository
{
    private readonly WriteDbContext _db;

    public ClassWriteRepository(WriteDbContext db)
    {
        _db = db;
    }

    public Task<Class?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Classes.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(Class entity) => _db.Classes.Add(entity);

    public void Update(Class entity) => _db.Classes.Update(entity);

    public void Remove(Class entity) => _db.Classes.Remove(entity);
}
