using DocFlow.Domain.Documents;

namespace DocFlow.Application.Documents;

public sealed record DocumentResponse(
    Guid Id,
    string OriginalFileName,
    string StoredFileName,
    string ContentType,
    long SizeBytes,
    string Checksum,
    string Status,
    DateTime UploadedAtUtc,
    DateTime? ProcessedAtUtc,
    DateTime? FailedAtUtc,
    string? FailureReason,
    int RetryCount,
    int MaxRetryCount,
    string? ExtractedTitle,
    string? ExtractedTextPreview,
    int? PageCount,
    string? MetadataJson);

public sealed record DocumentHistoryResponse(
    Guid Id,
    Guid DocumentId,
    string? FromStatus,
    string ToStatus,
    string Action,
    string? Reason,
    DateTime CreatedAtUtc);

internal static class DocumentMapper
{
    public static DocumentResponse ToResponse(Document document) => new(
        document.Id,
        document.OriginalFileName,
        document.StoredFileName,
        document.ContentType,
        document.SizeBytes,
        document.Checksum,
        document.Status.ToString(),
        document.UploadedAtUtc,
        document.ProcessedAtUtc,
        document.FailedAtUtc,
        document.FailureReason,
        document.RetryCount,
        document.MaxRetryCount,
        document.ExtractedTitle,
        document.ExtractedTextPreview,
        document.PageCount,
        document.MetadataJson);

    public static DocumentHistoryResponse ToHistoryResponse(DocumentProcessingHistory history) => new(
        history.Id,
        history.DocumentId,
        history.FromStatus?.ToString(),
        history.ToStatus.ToString(),
        history.Action,
        history.Reason,
        history.CreatedAtUtc);
}
