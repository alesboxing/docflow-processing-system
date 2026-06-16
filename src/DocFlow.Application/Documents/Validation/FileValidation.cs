using DocFlow.Application.Documents.Upload;
using DocFlow.Domain.Shared;

namespace DocFlow.Application.Documents.Validation;

public sealed class FileValidationOptions
{
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;
    public IReadOnlySet<string> AllowedExtensions { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx", ".txt" };
    public IReadOnlySet<string> AllowedContentTypes { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/pdf", "text/plain" };
}

public sealed class FileValidationPolicy
{
    private readonly FileValidationOptions _options;

    public FileValidationPolicy(FileValidationOptions options) => _options = options;

    public Result Validate(UploadDocumentCommand command)
    {
        if (command.Content is null) return Result.Failure(DocFlow.Application.ApplicationErrors.FileMissing);
        if (string.IsNullOrWhiteSpace(command.OriginalFileName)) return Result.Failure(DocFlow.Application.ApplicationErrors.FileNameRequired);
        if (string.IsNullOrWhiteSpace(command.ContentType)) return Result.Failure(DocFlow.Application.ApplicationErrors.ContentTypeRequired);
        if (command.SizeBytes <= 0) return Result.Failure(DocFlow.Application.ApplicationErrors.EmptyFile);
        if (command.SizeBytes > _options.MaxFileSizeBytes) return Result.Failure(DocFlow.Application.ApplicationErrors.FileTooLarge);

        var extension = Path.GetExtension(command.OriginalFileName);
        if (string.IsNullOrWhiteSpace(extension) || !_options.AllowedExtensions.Contains(extension)) return Result.Failure(DocFlow.Application.ApplicationErrors.UnsupportedFileExtension);
        if (!_options.AllowedContentTypes.Contains(command.ContentType) && extension != ".docx") return Result.Failure(DocFlow.Application.ApplicationErrors.UnsupportedContentType);

        return Result.Success();
    }
}
