namespace DocFlow.Domain.Shared;

public enum ErrorType
{
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5
}

public sealed record AppError(string Code, string Message, ErrorType Type);

public class Result
{
    protected Result(bool isSuccess, AppError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public AppError? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(AppError error) => new(false, error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(true, null) => _value = value;
    private Result(AppError error) : base(false, error) { }

    public T Value => IsSuccess && _value is not null
        ? _value
        : throw new InvalidOperationException("Result has no value.");

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(AppError error) => new(error);
}

namespace DocFlow.Domain.Documents;

using DocFlow.Domain.Shared;

public enum DocumentStatus
{
    Uploaded = 1,
    ValidationFailed = 2,
    Queued = 3,
    Processing = 4,
    Processed = 5,
    Failed = 6,
    Cancelled = 7
}

public sealed record ProcessingResult(
    string Title,
    string TextPreview,
    int PageCount,
    string? MetadataJson)
{
    public static Result<ProcessingResult> Create(
        string title,
        string textPreview,
        int pageCount,
        string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<ProcessingResult>.Failure(DocumentErrors.InvalidProcessingResult);

        if (title.Length > 300)
            return Result<ProcessingResult>.Failure(DocumentErrors.InvalidProcessingResult);

        if (string.IsNullOrWhiteSpace(textPreview))
            return Result<ProcessingResult>.Failure(DocumentErrors.InvalidProcessingResult);

        if (textPreview.Length > 2000)
            return Result<ProcessingResult>.Failure(DocumentErrors.InvalidProcessingResult);

        if (pageCount <= 0)
            return Result<ProcessingResult>.Failure(DocumentErrors.InvalidProcessingResult);

        if (metadataJson is not null && metadataJson.Length > 10000)
            return Result<ProcessingResult>.Failure(DocumentErrors.InvalidProcessingResult);

        return Result<ProcessingResult>.Success(new ProcessingResult(
            title.Trim(),
            textPreview.Trim(),
            pageCount,
            string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim()));
    }
}

public sealed class DocumentProcessingHistory
{
    private DocumentProcessingHistory() { }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public DocumentStatus? FromStatus { get; private set; }
    public DocumentStatus ToStatus { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    internal static DocumentProcessingHistory Create(
        Guid documentId,
        DocumentStatus? fromStatus,
        DocumentStatus toStatus,
        string action,
        string? reason,
        DateTime createdAtUtc)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(documentId));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required.", nameof(action));

        if (createdAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("History date must be UTC.", nameof(createdAtUtc));

        return new DocumentProcessingHistory
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Action = action.Trim(),
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CreatedAtUtc = createdAtUtc
        };
    }
}

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

public sealed class Document
{
    private readonly List<DocumentProcessingHistory> _history = new();

    private Document() { }

    public Guid Id { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StoredFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string Checksum { get; private set; } = string.Empty;
    public DocumentStatus Status { get; private set; }
    public DateTime UploadedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public DateTime? FailedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetryCount { get; private set; }
    public string? ExtractedTitle { get; private set; }
    public string? ExtractedTextPreview { get; private set; }
    public int? PageCount { get; private set; }
    public string? MetadataJson { get; private set; }
    public IReadOnlyCollection<DocumentProcessingHistory> History => _history.AsReadOnly();

    public static Result<Document> Create(
        string originalFileName,
        string storedFileName,
        string contentType,
        long sizeBytes,
        string checksum,
        DateTime uploadedAtUtc,
        int maxRetryCount = 3)
    {
        if (string.IsNullOrWhiteSpace(originalFileName)) return Result<Document>.Failure(DocumentErrors.InvalidFileName);
        if (string.IsNullOrWhiteSpace(storedFileName)) return Result<Document>.Failure(DocumentErrors.InvalidStoredFileName);
        if (string.IsNullOrWhiteSpace(contentType)) return Result<Document>.Failure(DocumentErrors.InvalidContentType);
        if (sizeBytes <= 0) return Result<Document>.Failure(DocumentErrors.InvalidSize);
        if (string.IsNullOrWhiteSpace(checksum)) return Result<Document>.Failure(DocumentErrors.InvalidChecksum);
        if (uploadedAtUtc.Kind != DateTimeKind.Utc) return Result<Document>.Failure(DocumentErrors.InvalidUtcDate);
        if (maxRetryCount is < 0 or > 10) return Result<Document>.Failure(DocumentErrors.MaxRetryReached);

        var document = new Document
        {
            Id = Guid.NewGuid(),
            OriginalFileName = originalFileName.Trim(),
            StoredFileName = storedFileName.Trim(),
            ContentType = contentType.Trim(),
            SizeBytes = sizeBytes,
            Checksum = checksum.Trim(),
            Status = DocumentStatus.Uploaded,
            UploadedAtUtc = uploadedAtUtc,
            RetryCount = 0,
            MaxRetryCount = maxRetryCount
        };

        document.AddHistory(null, DocumentStatus.Uploaded, "Document uploaded", null, uploadedAtUtc);
        return Result<Document>.Success(document);
    }

    public Result MarkQueued(DateTime queuedAtUtc)
    {
        if (queuedAtUtc.Kind != DateTimeKind.Utc) return Result.Failure(DocumentErrors.InvalidUtcDate);
        if (Status != DocumentStatus.Uploaded) return Result.Failure(DocumentErrors.InvalidStatusTransition);

        ChangeStatus(DocumentStatus.Queued, "Document queued for processing", null, queuedAtUtc);
        return Result.Success();
    }

    public Result StartProcessing(DateTime startedAtUtc)
    {
        if (startedAtUtc.Kind != DateTimeKind.Utc) return Result.Failure(DocumentErrors.InvalidUtcDate);
        if (Status != DocumentStatus.Queued) return Result.Failure(DocumentErrors.InvalidStatusTransition);

        ChangeStatus(DocumentStatus.Processing, "Document processing started", null, startedAtUtc);
        return Result.Success();
    }

    public Result MarkProcessed(ProcessingResult result, DateTime processedAtUtc)
    {
        if (processedAtUtc.Kind != DateTimeKind.Utc) return Result.Failure(DocumentErrors.InvalidUtcDate);
        if (Status != DocumentStatus.Processing) return Result.Failure(DocumentErrors.InvalidStatusTransition);

        ExtractedTitle = result.Title;
        ExtractedTextPreview = result.TextPreview;
        PageCount = result.PageCount;
        MetadataJson = result.MetadataJson;
        ProcessedAtUtc = processedAtUtc;
        FailedAtUtc = null;
        FailureReason = null;

        ChangeStatus(DocumentStatus.Processed, "Document processed successfully", null, processedAtUtc);
        return Result.Success();
    }

    public Result MarkFailed(string failureReason, DateTime failedAtUtc)
    {
        if (failedAtUtc.Kind != DateTimeKind.Utc) return Result.Failure(DocumentErrors.InvalidUtcDate);
        if (Status != DocumentStatus.Processing) return Result.Failure(DocumentErrors.InvalidStatusTransition);
        if (string.IsNullOrWhiteSpace(failureReason)) return Result.Failure(DocumentErrors.InvalidFailureReason);

        FailureReason = failureReason.Length > 1000 ? failureReason[..1000] : failureReason.Trim();
        FailedAtUtc = failedAtUtc;

        ChangeStatus(DocumentStatus.Failed, "Document processing failed", FailureReason, failedAtUtc);
        return Result.Success();
    }

    public Result Retry(string reason, DateTime retriedAtUtc)
    {
        if (retriedAtUtc.Kind != DateTimeKind.Utc) return Result.Failure(DocumentErrors.InvalidUtcDate);
        if (Status != DocumentStatus.Failed) return Result.Failure(DocumentErrors.InvalidStatusTransition);
        if (RetryCount >= MaxRetryCount) return Result.Failure(DocumentErrors.MaxRetryReached);
        if (string.IsNullOrWhiteSpace(reason)) return Result.Failure(DocumentErrors.InvalidFailureReason);

        RetryCount++;
        FailedAtUtc = null;
        FailureReason = null;
        ChangeStatus(DocumentStatus.Queued, "Document retry queued", reason, retriedAtUtc);
        return Result.Success();
    }

    public Result Cancel(string reason, DateTime cancelledAtUtc)
    {
        if (cancelledAtUtc.Kind != DateTimeKind.Utc) return Result.Failure(DocumentErrors.InvalidUtcDate);
        if (string.IsNullOrWhiteSpace(reason)) return Result.Failure(DocumentErrors.InvalidCancelReason);
        if (Status is not (DocumentStatus.Uploaded or DocumentStatus.Queued or DocumentStatus.Failed))
            return Result.Failure(DocumentErrors.InvalidStatusTransition);

        ChangeStatus(DocumentStatus.Cancelled, "Document cancelled", reason, cancelledAtUtc);
        return Result.Success();
    }

    private void ChangeStatus(DocumentStatus toStatus, string action, string? reason, DateTime createdAtUtc)
    {
        var fromStatus = Status;
        Status = toStatus;
        AddHistory(fromStatus, toStatus, action, reason, createdAtUtc);
    }

    private void AddHistory(DocumentStatus? fromStatus, DocumentStatus toStatus, string action, string? reason, DateTime createdAtUtc)
    {
        _history.Add(DocumentProcessingHistory.Create(Id, fromStatus, toStatus, action, reason, createdAtUtc));
    }
}
