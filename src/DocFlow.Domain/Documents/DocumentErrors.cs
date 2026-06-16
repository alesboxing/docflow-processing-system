using DocFlow.Domain.Shared;

namespace DocFlow.Domain.Documents;

public static class DocumentErrors
{
    public static readonly AppError InvalidFileName = new("Document.InvalidFileName", "File name is required.", ErrorType.Validation);
    public static readonly AppError InvalidStoredFileName = new("Document.InvalidStoredFileName", "Stored file name is required.", ErrorType.Validation);
    public static readonly AppError InvalidContentType = new("Document.InvalidContentType", "Content type is required.", ErrorType.Validation);
    public static readonly AppError InvalidSize = new("Document.InvalidSize", "File size must be greater than zero.", ErrorType.Validation);
    public static readonly AppError InvalidChecksum = new("Document.InvalidChecksum", "Checksum is required.", ErrorType.Validation);
    public static readonly AppError InvalidUtcDate = new("Document.InvalidUtcDate", "Date must be UTC.", ErrorType.Validation);
    public static readonly AppError InvalidStatusTransition = new("Document.InvalidStatusTransition", "Document status transition is not allowed.", ErrorType.Conflict);
    public static readonly AppError InvalidFailureReason = new("Document.InvalidFailureReason", "Failure reason is required.", ErrorType.Validation);
    public static readonly AppError MaxRetryReached = new("Document.MaxRetryReached", "Maximum retry count has been reached.", ErrorType.Conflict);
    public static readonly AppError InvalidCancelReason = new("Document.InvalidCancelReason", "Cancel reason is required.", ErrorType.Validation);
    public static readonly AppError InvalidProcessingResult = new("Document.InvalidProcessingResult", "Processing result is invalid.", ErrorType.Validation);
}
