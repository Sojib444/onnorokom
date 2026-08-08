namespace AssignmentManagement.Domain.Enums;

/// <summary>
/// The roles available in the system. The API derives the role of an authenticated
/// caller from the validated JWT, never from values supplied by the frontend.
/// </summary>
public enum UserRole
{
    /// <summary>System administrator: manages users, classes, subjects and allocations.</summary>
    Admin = 0,

    /// <summary>Teacher: authors assignments and grades submissions for allocated class/subject pairs.</summary>
    Teacher = 1,

    /// <summary>Student: submits answers for assignments targeted at their class.</summary>
    Student = 2,
}
