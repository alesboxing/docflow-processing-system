namespace DocFlow.Domain.Documents;

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
