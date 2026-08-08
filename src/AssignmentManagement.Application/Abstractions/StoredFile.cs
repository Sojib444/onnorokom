namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Metadata describing a stored file so the caller can persist it without holding the
/// bytes in memory or in the database.
/// </summary>
public sealed record StoredFile(string Path, long Size);
