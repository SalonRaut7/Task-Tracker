namespace TaskTracker.Application.Interfaces;

public record FileUploadResult(string PublicId, string SecureUrl, string ResourceType);

public interface IFileStorageService
{
    /// Uploads a file to cloud storage.
    Task<FileUploadResult> UploadAsync(
        byte[] fileData,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// Deletes a file from cloud storage by its public ID.
    Task DeleteAsync(
        string publicId,
        string resourceType,
        CancellationToken cancellationToken = default);
}
