using DocFlow.Domain.Documents;

namespace DocFlow.Application.Abstractions;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken ct = default);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetPagedAsync(int skip, int take, DocumentStatus? status, CancellationToken ct = default);
    Task<int> CountAsync(DocumentStatus? status, CancellationToken ct = default);
}

public sealed record StoredFileInfo(string StoredFileName, long SizeBytes);

public interface IFileStorage
{
    Task<StoredFileInfo> SaveAsync(Stream fileStream, string originalFileName, string contentType, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storedFileName, CancellationToken ct = default);
    Task DeleteAsync(string storedFileName, CancellationToken ct = default);
}

public interface IBackgroundJobClient
{
    Task EnqueueDocumentProcessingAsync(Guid documentId, CancellationToken ct = default);
}

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

public interface IChecksumService
{
    Task<string> CalculateSha256Async(Stream stream, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IDocumentProcessor
{
    Task<ProcessingResult> ProcessAsync(Stream fileStream, string originalFileName, string contentType, CancellationToken ct = default);
}
