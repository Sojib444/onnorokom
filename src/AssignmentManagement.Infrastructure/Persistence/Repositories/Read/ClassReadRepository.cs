using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Persistence.Repositories.Read;

/// <summary>
/// EF Core-backed implementation of <see cref="IClassReadRepository"/>. Queries always
/// run untracked.
/// </summary>
public sealed class ClassReadRepository : IClassReadRepository
{
    private readonly ReadDbContext _db;

    public ClassReadRepository(ReadDbContext db)
    {
        _db = db;
    }

    public Task<Class?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Classes.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<IReadOnlyList<Class>> GetAllAsync(CancellationToken cancellationToken) =>
        _db.Classes.AsNoTracking().OrderBy(c => c.Name).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Class>)t.Result, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Classes.AnyAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) =>
        _db.Classes.AnyAsync(c => c.Name == name, cancellationToken);
}
