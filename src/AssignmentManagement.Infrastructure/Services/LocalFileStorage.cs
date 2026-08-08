using System.Security.Cryptography;
using System.Text;
using AssignmentManagement.Application.Abstractions;

namespace AssignmentManagement.Infrastructure.Services;

/// <summary>
/// Local-disk implementation of <see cref="IFileStorage"/>. Files are written under a
/// configured root directory using a unique, non-guessable key so that stored file
/// names can never collide or escape the root. The production path for a multi-instance
/// deployment is object storage behind the same interface.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(string rootPath)
    {
        _rootPath = rootPath;
    }

    public async Task<StoredFile> SaveAsync(
        string folder,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var directory = Path.Combine(_rootPath, folder);
        Directory.CreateDirectory(directory);

        var key = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var fullPath = Path.Combine(directory, key);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, cancellationToken);
        var size = file.Length;

        var relativePath = Path.Combine(folder, key).Replace('\\', '/');
        return new StoredFile(relativePath, size);
    }

    public Task<Stream?> GetAsync(string path, CancellationToken cancellationToken)
    {
        var fullPath = ResolveSafePath(path);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        var fullPath = ResolveSafePath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Resolves a stored key against the root, refusing anything that would escape the
    /// storage root (e.g. a key containing path segments).
    /// </summary>
    private string ResolveSafePath(string path)
    {
        var normalized = (path ?? string.Empty).Replace('/', Path.DirectorySeparatorChar);
        if (normalized.Contains(".."))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalized));
        var rootFullPath = Path.GetFullPath(_rootPath);

        if (!fullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }

        return fullPath;
    }
}
