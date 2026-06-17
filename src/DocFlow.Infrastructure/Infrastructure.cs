using System.Security.Cryptography;
using DocFlow.Application.Abstractions;
using DocFlow.Domain.Documents;
using DocFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocFlow.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=docflow;Username=postgres;Password=postgres";

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddScoped<IChecksumService, ChecksumService>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IDocumentProcessor, FakeDocumentProcessor>();
        services.AddScoped<IBackgroundJobClient, NoOpBackgroundJobClient>();
        return services;
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
