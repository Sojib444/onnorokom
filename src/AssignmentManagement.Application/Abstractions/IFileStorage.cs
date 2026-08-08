namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Stores and retrieves submission attachments on a local volume behind this interface.
/// Object storage would be the production path; swapping implementations is confined to
/// this boundary.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Persists a file inside <paramref name="folder"/> under a unique key derived from
    /// the original file name, and returns the storage path and size for persistence.
    /// </summary>
    Task<StoredFile> SaveAsync(
        string folder,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken);

    /// <summary>Opens a previously saved file for reading, or null when missing.</summary>
    Task<Stream?> GetAsync(string path, CancellationToken cancellationToken);

    /// <summary>Deletes a previously saved file, if it exists.</summary>
    Task DeleteAsync(string path, CancellationToken cancellationToken);
}
