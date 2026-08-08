namespace AssignmentManagement.Application.Contracts;

/// <summary>A subject taught at the institution.</summary>
public sealed record SubjectResponse(Guid Id, string Name, string Code);

/// <summary>Body for creating a subject.</summary>
public sealed record CreateSubjectRequest(string Name, string Code);

/// <summary>Body for updating a subject.</summary>
public sealed record UpdateSubjectRequest(string Name, string Code);
