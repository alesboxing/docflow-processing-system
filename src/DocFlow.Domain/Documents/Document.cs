using DocFlow.Domain.Shared;

namespace DocFlow.Domain.Documents;

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
    public IReadOnlyCollection<DocumentProcessingHistory> History => _history;

    public static Result<Document> Create(string originalFileName, string storedFileName, string contentType, long sizeBytes, string checksum, DateTime uploadedAtUtc, int maxRetryCount = 3)
    {
        if (string.IsNullOrWhiteSpace(originalFileName)) return Result<Document>.Failure(DocumentErrors.InvalidFileName);
        if (string.IsNullOrWhiteSpace(storedFileName)) return Result<Document>.Failure(DocumentErrors.InvalidStoredFileName);
        if (string.IsNullOrWhiteSpace(contentType)) return Result<Document>.Failure(DocumentErrors.InvalidContentType);
        if (sizeBytes <= 0) return Result<Document>.Failure(DocumentErrors.InvalidSize);
        if (string.IsNullOrWhiteSpace(checksum)) return Result<Document>.Failure(DocumentErrors.InvalidChecksum);
        if (uploadedAtUtc.Kind != DateTimeKind.Utc) return Result<Document>.Failure(DocumentErrors.InvalidUtcDate);

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
        FailureReason = failureReason.Trim();
        FailedAtUtc = failedAtUtc;
        ChangeStatus(DocumentStatus.Failed, "Document processing failed", FailureReason, failedAtUtc);
        return Result.Success();
    }

    public Result Retry(string reason, DateTime retriedAtUtc)
    {
        if (retriedAtUtc.Kind != DateTimeKind.Utc) return Result.Failure(DocumentErrors.InvalidUtcDate);
        if (Status != DocumentStatus.Failed) return Result.Failure(DocumentErrors.InvalidStatusTransition);
        if (RetryCount >= MaxRetryCount) return Result.Failure(DocumentErrors.MaxRetryReached);
        RetryCount++;
        FailedAtUtc = null;
        FailureReason = null;
        ChangeStatus(DocumentStatus.Queued, "Document retry queued", reason, retriedAtUtc);
        return Result.Success();
    }

    public Result Cancel(string reason, DateTime cancelledAtUtc)
    {
        if (cancelledAtUtc.Kind != DateTimeKind.Utc) return Result.Failure(DocumentErrors.InvalidUtcDate);
        if (Status is not (DocumentStatus.Uploaded or DocumentStatus.Queued or DocumentStatus.Failed)) return Result.Failure(DocumentErrors.InvalidStatusTransition);
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
