using DocFlow.Application.Abstractions;
using DocFlow.Domain.Shared;

namespace DocFlow.Application.Documents.Download;

public sealed class DocumentDownloadResponse
{
    public DocumentDownloadResponse(Stream content, string fileName, string contentType, long sizeBytes)
    {
        Content = content;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
    }

    public Stream Content { get; }
    public string FileName { get; }
    public string ContentType { get; }
    public long SizeBytes { get; }
}

public interface IDocumentDownloadService
{
    Task<Result<DocumentDownloadResponse>> DownloadAsync(Guid documentId, CancellationToken ct = default);
}

public sealed class DocumentDownloadService : IDocumentDownloadService
{
    private readonly IDocumentRepository _documents;
    private readonly IFileStorage _storage;

    public DocumentDownloadService(IDocumentRepository documents, IFileStorage storage)
    {
        _documents = documents;
        _storage = storage;
    }

    public async Task<Result<DocumentDownloadResponse>> DownloadAsync(Guid documentId, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) return Result<DocumentDownloadResponse>.Failure(DocFlow.Application.ApplicationErrors.InvalidDocumentId);
        var document = await _documents.GetByIdAsync(documentId, ct);
        if (document is null) return Result<DocumentDownloadResponse>.Failure(DocFlow.Application.ApplicationErrors.DocumentNotFound);
        var stream = await _storage.OpenReadAsync(document.StoredFileName, ct);
        return Result<DocumentDownloadResponse>.Success(new DocumentDownloadResponse(stream, document.OriginalFileName, document.ContentType, document.SizeBytes));
    }
}
