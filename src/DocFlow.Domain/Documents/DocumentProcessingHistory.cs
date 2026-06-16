namespace DocFlow.Domain.Documents;

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

    internal static DocumentProcessingHistory Create(Guid documentId, DocumentStatus? fromStatus, DocumentStatus toStatus, string action, string? reason, DateTime createdAtUtc)
    {
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
