namespace AssignmentManagement.Application.Contracts;

/// <summary>An assignment with its class and subject names resolved for display.</summary>
public sealed record AssignmentResponse(
    Guid Id,
    Guid TeacherId,
    Guid ClassId,
    string? ClassName,
    Guid SubjectId,
    string? SubjectName,
    string Title,
    string Description,
    DateTimeOffset Deadline,
    decimal MaximumMarks,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Body for creating a draft assignment.</summary>
public sealed record CreateAssignmentRequest(
    Guid ClassId,
    Guid SubjectId,
    string Title,
    string Description,
    DateTimeOffset Deadline,
    decimal MaximumMarks);

/// <summary>Body for updating a draft assignment.</summary>
public sealed record UpdateAssignmentRequest(
    Guid ClassId,
    Guid SubjectId,
    string Title,
    string Description,
    DateTimeOffset Deadline,
    decimal MaximumMarks);
