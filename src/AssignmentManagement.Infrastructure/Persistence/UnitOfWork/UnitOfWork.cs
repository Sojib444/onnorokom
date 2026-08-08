using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Infrastructure.Persistence.Context;

namespace AssignmentManagement.Infrastructure.Persistence.UnitOfWork;

/// <summary>
/// EF Core-backed implementation of <see cref="IUnitOfWork"/>. One save call commits all
/// write-repository changes made during a command through the tracking
/// <see cref="WriteDbContext"/>, and that context dispatches any pending domain events as
/// part of the same operation.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly WriteDbContext _db;

    public UnitOfWork(WriteDbContext db)
    {
        _db = db;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);
}
