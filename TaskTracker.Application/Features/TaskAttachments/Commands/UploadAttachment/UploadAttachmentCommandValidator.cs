using FluentValidation;

namespace TaskTracker.Application.Features.TaskAttachments.Commands.UploadAttachment;

public class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    private const long MaxFileSizeBytes = 10L * 1024 * 1024; // 10 MB

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".pdf",
        ".doc", ".docx",
        ".xls", ".xlsx",
        ".jpg", ".jpeg", ".png", ".gif",
    };

    public UploadAttachmentCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .GreaterThan(0).WithMessage("TaskId must be a positive integer.");

        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("FileName is required.")
            .MaximumLength(255).WithMessage("FileName cannot exceed 255 characters.")
            .Must(HaveAllowedExtension)
            .WithMessage($"File type is not allowed. Allowed extensions: {string.Join(", ", AllowedExtensions)}");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("File is empty.")
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage($"File size exceeds the maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("ContentType is required.");
    }

    private static bool HaveAllowedExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension);
    }
}
