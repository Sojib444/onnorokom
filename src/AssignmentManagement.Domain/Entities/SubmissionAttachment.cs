using AssignmentManagement.Domain.Common;

namespace AssignmentManagement.Domain.Entities;

/// <summary>
/// A file attached to a submission. The file's bytes live behind a storage service; the
/// entity only records the metadata and the storage key needed to retrieve it.
/// </summary>
public sealed class SubmissionAttachment : Entity
{
    /// <summary>The submission this file belongs to.</summary>
    public Guid SubmissionId { get; private set; }

    /// <summary>The original file name as uploaded by the student.</summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>Opaque key identifying the stored bytes, e.g. a relative path or object key.</summary>
    public string StoragePath { get; private set; } = string.Empty;

    /// <summary>MIME type of the file, used when downloading.</summary>
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>File size in bytes.</summary>
    public long Size { get; private set; }

    /// <summary>Persistence-only constructor for EF Core materialization.</summary>
    private SubmissionAttachment()
    {
    }

    /// <summary>
    /// Creates an attachment record. The storage path must already be valid, meaning the
    /// file was successfully written before this record is created.
    /// </summary>
    public SubmissionAttachment(
        Guid submissionId,
        string fileName,
        string storagePath,
        string contentType,
        long size)
    {
        SubmissionId = submissionId;
        FileName = fileName;
        StoragePath = storagePath;
        ContentType = contentType;
        Size = size;
    }
}
