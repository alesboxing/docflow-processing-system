using DocFlow.Domain.Documents;

namespace DocFlow.Api;

public sealed class UploadDocumentFormRequest
{
    public IFormFile? File { get; init; }
}

public sealed record RetryDocumentRequest(string Reason);
public sealed record CancelDocumentRequest(string Reason);
public sealed record GetDocumentsQuery(int Page = 1, int PageSize = 20, DocumentStatus? Status = null);
public sealed record ErrorResponse(string Code, string Message, string Type);
