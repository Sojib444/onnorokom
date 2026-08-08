namespace AssignmentManagement.IntegrationTests;

public sealed record LoginResponseDto(
    string Token,
    string TokenType,
    DateTimeOffset ExpiresAt,
    AuthenticatedUserDto User);

public sealed record AuthenticatedUserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    Guid? ClassId);

public sealed record AssignmentDto(
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

public sealed record SubmissionDto(
    Guid Id,
    Guid AssignmentId,
    string? AssignmentTitle,
    Guid StudentId,
    string? StudentName,
    string Answer,
    string Status,
    decimal? Marks,
    string? Feedback,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? GradedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<SubmissionAttachmentDto> Attachments);

public sealed record SubmissionAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long Size);

public sealed record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    Guid? ClassId,
    string? ClassName,
    DateTimeOffset CreatedAt);

public sealed record ClassDto(Guid Id, string Name, string? Description);

public sealed record SubjectDto(Guid Id, string Name, string Code);

public sealed record TeacherAssignmentDto(
    Guid Id,
    Guid TeacherId,
    string TeacherName,
    Guid ClassId,
    string ClassName,
    Guid SubjectId,
    string SubjectName);
