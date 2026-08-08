using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Persistence.Repositories.Read;

/// <summary>
/// EF Core-backed implementation of <see cref="ISubjectReadRepository"/>. Queries
/// always run untracked.
/// </summary>
public sealed class SubjectReadRepository : ISubjectReadRepository
{
    private readonly ReadDbContext _db;

    public SubjectReadRepository(ReadDbContext db)
    {
        _db = db;
    }

    public Task<Subject?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Subjects.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken cancellationToken) =>
        _db.Subjects.AsNoTracking().OrderBy(s => s.Name).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Subject>)t.Result, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken) =>
        _db.Subjects.AnyAsync(s => s.Code == code, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Subjects.AnyAsync(s => s.Id == id, cancellationToken);
}
