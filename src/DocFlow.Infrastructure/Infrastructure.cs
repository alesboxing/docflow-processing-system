using System.Security.Cryptography;
using DocFlow.Application.Abstractions;
using DocFlow.Domain.Documents;
using Microsoft.Extensions.DependencyInjection;

namespace DocFlow.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddScoped<IChecksumService, ChecksumService>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IDocumentProcessor, FakeDocumentProcessor>();
        services.AddScoped<IBackgroundJobClient, NoOpBackgroundJobClient>();
        services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
        return services;
    }
}

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly List<Document> _documents = new();
    private readonly object _sync = new();

    public Task AddAsync(Document document, CancellationToken ct = default)
    {
        lock (_sync) _documents.Add(document);
        return Task.CompletedTask;
    }

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        lock (_sync) return Task.FromResult(_documents.FirstOrDefault(x => x.Id == id));
    }

    public Task<IReadOnlyList<Document>> GetPagedAsync(int skip, int take, DocumentStatus? status, CancellationToken ct = default)
    {
        lock (_sync)
        {
            var query = _documents.AsEnumerable();
            if (status.HasValue) query = query.Where(x => x.Status == status.Value);
            return Task.FromResult<IReadOnlyList<Document>>(query.OrderByDescending(x => x.UploadedAtUtc).Skip(skip).Take(take).ToList());
        }
    }

    public Task<int> CountAsync(DocumentStatus? status, CancellationToken ct = default)
    {
        lock (_sync)
        {
            var count = status.HasValue ? _documents.Count(x => x.Status == status.Value) : _documents.Count;
            return Task.FromResult(count);
        }
    }
}

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath = Path.Combine(AppContext.BaseDirectory, "storage", "documents");

    public async Task<StoredFileInfo> SaveAsync(Stream fileStream, string originalFileName, string contentType, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_rootPath);
        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_rootPath, storedFileName);
        await using var output = File.Create(fullPath);
        await fileStream.CopyToAsync(output, ct);
        return new StoredFileInfo(storedFileName, new FileInfo(fullPath).Length);
    }

    public Task<Stream> OpenReadAsync(string storedFileName, CancellationToken ct = default)
    {
        var safeName = Path.GetFileName(storedFileName);
        var fullPath = Path.Combine(_rootPath, safeName);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken ct = default)
    {
        var safeName = Path.GetFileName(storedFileName);
        var fullPath = Path.Combine(_rootPath, safeName);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }
}

public sealed class ChecksumService : IChecksumService
{
    public async Task<string> CalculateSha256Async(Stream stream, CancellationToken ct = default)
    {
        if (stream.CanSeek) stream.Position = 0;
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, ct);
        if (stream.CanSeek) stream.Position = 0;
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public sealed class FakeDocumentProcessor : IDocumentProcessor
{
    public Task<ProcessingResult> ProcessAsync(Stream fileStream, string originalFileName, string contentType, CancellationToken ct = default)
    {
        var title = Path.GetFileNameWithoutExtension(originalFileName);
        var result = ProcessingResult.Create(
            string.IsNullOrWhiteSpace(title) ? "Untitled document" : title,
            "Fake extracted text preview for demo processing workflow.",
            1,
            "{ \"processor\": \"FakeDocumentProcessor\" }");
        return Task.FromResult(result.Value);
    }
}

public sealed class NoOpBackgroundJobClient : IBackgroundJobClient
{
    public Task EnqueueDocumentProcessingAsync(Guid documentId, CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class NoOpUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
