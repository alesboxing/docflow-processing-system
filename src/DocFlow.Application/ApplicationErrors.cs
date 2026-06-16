using DocFlow.Domain.Shared;

namespace DocFlow.Application;

public static class ApplicationErrors
{
    public static readonly AppError FileMissing = new("File.Missing", "File is required.", ErrorType.Validation);
    public static readonly AppError FileNameRequired = new("File.FileNameRequired", "File name is required.", ErrorType.Validation);
    public static readonly AppError ContentTypeRequired = new("File.ContentTypeRequired", "Content type is required.", ErrorType.Validation);
    public static readonly AppError EmptyFile = new("File.Empty", "File cannot be empty.", ErrorType.Validation);
    public static readonly AppError FileTooLarge = new("File.TooLarge", "File size exceeds the allowed limit.", ErrorType.Validation);
    public static readonly AppError UnsupportedFileExtension = new("File.UnsupportedExtension", "File extension is not supported.", ErrorType.Validation);
    public static readonly AppError UnsupportedContentType = new("File.UnsupportedContentType", "File content type is not supported.", ErrorType.Validation);
    public static readonly AppError DocumentNotFound = new("Document.NotFound", "Document was not found.", ErrorType.NotFound);
    public static readonly AppError InvalidDocumentId = new("Document.InvalidId", "Document id is invalid.", ErrorType.Validation);
    public static readonly AppError InvalidPage = new("Pagination.InvalidPage", "Page must be greater than or equal to 1.", ErrorType.Validation);
    public static readonly AppError InvalidPageSize = new("Pagination.InvalidPageSize", "PageSize must be between 1 and 100.", ErrorType.Validation);
    public static readonly AppError RetryReasonRequired = new("Document.RetryReasonRequired", "Retry reason is required.", ErrorType.Validation);
    public static readonly AppError CancelReasonRequired = new("Document.CancelReasonRequired", "Cancel reason is required.", ErrorType.Validation);
    public static readonly AppError StoredFileNotFound = new("File.StoredFileNotFound", "Stored file was not found.", ErrorType.NotFound);
    public static readonly AppError ProcessingFailed = new("Document.ProcessingFailed", "Document processing failed.", ErrorType.Conflict);
}
