using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.ValueObjects;
using AssignmentManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Persistence.Repositories.Read;

/// <summary>
/// EF Core-backed implementation of <see cref="IUserReadRepository"/>. Queries always
/// run untracked and return plain projections for display.
/// </summary>
public sealed class UserReadRepository : IUserReadRepository
{
    private readonly ReadDbContext _db;

    public UserReadRepository(ReadDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        _db.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == new EmailAddress(email), cancellationToken);

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken) =>
        _db.Users.AsNoTracking().OrderBy(u => u.CreatedAt).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<User>)t.Result, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) =>
        _db.Users.AnyAsync(u => u.Email == new EmailAddress(email), cancellationToken);

    public Task<bool> ExistsStudentInClassAsync(
        Guid classId,
        CancellationToken cancellationToken) =>
        _db.Users.AnyAsync(
            u => u.ClassId == classId && u.Role == Domain.Enums.UserRole.Student,
            cancellationToken);
}
