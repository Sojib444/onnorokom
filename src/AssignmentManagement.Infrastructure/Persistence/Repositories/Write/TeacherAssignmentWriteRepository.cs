using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Persistence.Repositories.Write;

/// <summary>
/// EF Core-backed implementation of <see cref="ITeacherAssignmentWriteRepository"/>.
/// Loaded aggregates stay tracked by <see cref="WriteDbContext"/> so deletions persist
/// with the unit of work.
/// </summary>
public sealed class TeacherAssignmentWriteRepository : ITeacherAssignmentWriteRepository
{
    private readonly WriteDbContext _db;

    public TeacherAssignmentWriteRepository(WriteDbContext db)
    {
        _db = db;
    }

    public Task<TeacherAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.TeacherAssignments.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

    public void Add(TeacherAssignment entity) => _db.TeacherAssignments.Add(entity);

    public void Remove(TeacherAssignment entity) => _db.TeacherAssignments.Remove(entity);
}
