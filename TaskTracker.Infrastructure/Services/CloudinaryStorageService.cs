using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Options;

namespace TaskTracker.Infrastructure.Services;

public class CloudinaryStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryStorageService> _logger;

    private static readonly HashSet<string> ImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif",
    };

    public CloudinaryStorageService(
        IOptions<CloudinaryOptions> options,
        ILogger<CloudinaryStorageService> logger)
    {
        _logger = logger;

        var config = options.Value;
        var account = new Account(config.CloudName, config.ApiKey, config.ApiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<FileUploadResult> UploadAsync(
        byte[] fileData,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var isImage = ImageContentTypes.Contains(contentType);
        using var stream = new MemoryStream(fileData);

        if (isImage)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = "tasktracker/attachments",
                UseFilename = true,
                UniqueFilename = true,
            };

            var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

            if (result.Error != null)
            {
                _logger.LogError("Cloudinary image upload failed: {Error}", result.Error.Message);
                throw new InvalidOperationException($"File upload failed: {result.Error.Message}");
            }

            return new FileUploadResult(result.PublicId, result.SecureUrl.ToString(), "image");
        }
        else
        {
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = "tasktracker/attachments",
                UseFilename = true,
                UniqueFilename = true,
            };

            var result = await _cloudinary.UploadLargeRawAsync(uploadParams, cancellationToken: cancellationToken);

            if (result.Error != null)
            {
                _logger.LogError("Cloudinary raw upload failed: {Error}", result.Error.Message);
                throw new InvalidOperationException($"File upload failed: {result.Error.Message}");
            }

            return new FileUploadResult(result.PublicId, result.SecureUrl.ToString(), "raw");
        }
    }

    public async Task DeleteAsync(
        string publicId,
        string resourceType,
        CancellationToken cancellationToken = default)
    {
        var deletionParams = new DeletionParams(publicId)
        {
            ResourceType = resourceType == "image"
                ? CloudinaryDotNet.Actions.ResourceType.Image
                : CloudinaryDotNet.Actions.ResourceType.Raw,
        };

        var result = await _cloudinary.DestroyAsync(deletionParams);

        if (result.Error != null)
        {
            _logger.LogWarning(
                "Cloudinary deletion failed for {PublicId}: {Error}",
                publicId,
                result.Error.Message);
        }
    }
}
