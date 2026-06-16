using DocFlow.Application.Documents;
using DocFlow.Domain.Shared;

namespace DocFlow.Application.Documents.Processing;

public sealed record RetryDocumentCommand(string Reason);
public sealed record CancelDocumentCommand(string Reason);

public interface IDocumentProcessingService
{
    Task<Result<DocumentResponse>> ProcessAsync(Guid documentId, CancellationToken ct = default);
    Task<Result<DocumentResponse>> RetryAsync(Guid documentId, RetryDocumentCommand command, CancellationToken ct = default);
    Task<Result<DocumentResponse>> CancelAsync(Guid documentId, CancelDocumentCommand command, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DocumentHistoryResponse>>> GetHistoryAsync(Guid documentId, CancellationToken ct = default);
}
