namespace AssignmentManagement.Application.Contracts;

/// <summary>A file attached to a submission; metadata only, the bytes live in storage.</summary>
public sealed record SubmissionAttachmentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long Size);

/// <summary>A submission with the assignment title and student name resolved for display.</summary>
public sealed record SubmissionResponse(
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
    IReadOnlyList<SubmissionAttachmentResponse> Attachments);

/// <summary>Body for grading a submission.</summary>
public sealed record GradeSubmissionRequest(decimal Marks, string? Feedback);

/// <summary>A stored attachment opened for download.</summary>
public sealed record AttachmentDownloadResponse(
    Stream Content,
    string FileName,
    string ContentType);
