using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Persistence.Repositories.Write;

/// <summary>
/// EF Core-backed implementation of <see cref="IUserWriteRepository"/>. Loaded
/// aggregates stay tracked by <see cref="WriteDbContext"/> so profile and password
/// changes persist with the unit of work.
/// </summary>
public sealed class UserWriteRepository : IUserWriteRepository
{
    private readonly WriteDbContext _db;

    public UserWriteRepository(WriteDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public void Add(User user) => _db.Users.Add(user);

    public void Update(User user) => _db.Users.Update(user);

    public void Remove(User user) => _db.Users.Remove(user);
}
