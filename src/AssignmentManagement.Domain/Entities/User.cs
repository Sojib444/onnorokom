using AssignmentManagement.Domain.Common;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.Exceptions;
using AssignmentManagement.Domain.ValueObjects;

namespace AssignmentManagement.Domain.Entities;

/// <summary>
/// A user of the system. Every user has exactly one role; a student optionally belongs
/// to a class. Teachers and administrators are not tied to a class through this entity
/// — teacher-to-class/subject allocation lives in <see cref="TeacherAssignment"/>.
/// </summary>
/// <remarks>
/// Invariants:
/// <list type="bullet">
/// <item>Full name is required.</item>
/// <item>Email is required and valid (see <see cref="EmailAddress"/>).</item>
/// <item>A password hash is required before the account can be used.</item>
/// <item>Only students carry a class affiliation; it is optional.</item>
/// </list>
/// The password is never stored or exposed as plaintext, only a hash set by
/// <see cref="SetPasswordHash"/>.
/// </remarks>
public sealed class User : AggregateRoot, IAuditable
{
    /// <summary>Full display name of the user.</summary>
    public string FullName { get; private set; } = string.Empty;

    /// <summary>Validated, normalized email address used for login.</summary>
    public EmailAddress Email { get; private set; } = null!;

    /// <summary>Bcrypt hash of the user's password. Never the plaintext password.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>The role that governs what the user may do in the system.</summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// The class a student belongs to. Null for administrators and teachers, and
    /// optional for students (assumption: one class per student).
    /// </summary>
    public Guid? ClassId { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Persistence-only constructor for EF Core materialization.</summary>
    private User()
    {
    }

    /// <summary>
    /// Creates a user with the given role. The account is not usable until
    /// <see cref="SetPasswordHash"/> has been called.
    /// </summary>
    /// <param name="fullName">Display name; required.</param>
    /// <param name="email">Email address used for login.</param>
    /// <param name="role">The user's role.</param>
    /// <param name="classId">Optional class for a student.</param>
    /// <param name="now">Current UTC timestamp for audit fields.</param>
    public User(string fullName, EmailAddress email, UserRole role, Guid? classId, DateTimeOffset now)
    {
        FullName = NormalizeName(fullName);
        Email = email;
        Role = role;
        ClassId = role == UserRole.Student ? classId : null;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Sets (or replaces) the password hash. Enforced by the authentication service
    /// which produces the hash; the domain refuses empty values.
    /// </summary>
    public void SetPasswordHash(string passwordHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new BusinessRuleViolation("A password hash is required.");
        }

        PasswordHash = passwordHash;
        UpdatedAt = now;
    }

    /// <summary>
    /// Updates the display name and, for students, the class affiliation.
    /// </summary>
    public void UpdateProfile(string fullName, Guid? classId, DateTimeOffset now)
    {
        FullName = NormalizeName(fullName);
        ClassId = Role == UserRole.Student ? classId : null;
        UpdatedAt = now;
    }

    private static string NormalizeName(string fullName)
    {
        var normalized = (fullName ?? string.Empty).Trim();

        if (normalized.Length == 0)
        {
            throw new BusinessRuleViolation("A full name is required.");
        }

        return normalized;
    }
}
