namespace AssignmentManagement.Application.Contracts;

/// <summary>A class (or course) that students belong to and assignments target.</summary>
public sealed record ClassResponse(Guid Id, string Name, string? Description);

/// <summary>Body for creating a class.</summary>
public sealed record CreateClassRequest(string Name, string? Description);

/// <summary>Body for updating a class.</summary>
public sealed record UpdateClassRequest(string Name, string? Description);
