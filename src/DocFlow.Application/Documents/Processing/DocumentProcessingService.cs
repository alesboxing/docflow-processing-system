using DocFlow.Application.Abstractions;
using DocFlow.Application.Documents;
using DocFlow.Domain.Shared;

namespace DocFlow.Application.Documents.Processing;

public sealed class DocumentProcessingService : IDocumentProcessingService
{
    private readonly IDocumentRepository _documents;
    private readonly IFileStorage _storage;
    private readonly IDocumentProcessor _processor;
    private readonly IBackgroundJobClient _jobs;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    public DocumentProcessingService(IDocumentRepository documents, IFileStorage storage, IDocumentProcessor processor, IBackgroundJobClient jobs, IDateTimeProvider clock, IUnitOfWork unitOfWork)
    {
        _documents = documents;
        _storage = storage;
        _processor = processor;
        _jobs = jobs;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentResponse>> ProcessAsync(Guid documentId, CancellationToken ct = default)
    {
        var document = await _documents.GetByIdAsync(documentId, ct);
        if (document is null) return Result<DocumentResponse>.Failure(DocFlow.Application.ApplicationErrors.DocumentNotFound);

        var start = document.StartProcessing(_clock.UtcNow);
        if (start.IsFailure) return Result<DocumentResponse>.Failure(start.Error!);

        try
        {
            await using var stream = await _storage.OpenReadAsync(document.StoredFileName, ct);
            var result = await _processor.ProcessAsync(stream, document.OriginalFileName, document.ContentType, ct);
            var processed = document.MarkProcessed(result, _clock.UtcNow);
            if (processed.IsFailure) return Result<DocumentResponse>.Failure(processed.Error!);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var failed = document.MarkFailed(BuildFailureReason(exception), _clock.UtcNow);
            if (failed.IsFailure) return Result<DocumentResponse>.Failure(failed.Error!);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<DocumentResponse>.Success(DocumentMapper.ToResponse(document));
    }

    public async Task<Result<DocumentResponse>> RetryAsync(Guid documentId, RetryDocumentCommand command, CancellationToken ct = default)
    {
        var document = await _documents.GetByIdAsync(documentId, ct);
        if (document is null) return Result<DocumentResponse>.Failure(DocFlow.Application.ApplicationErrors.DocumentNotFound);
        if (string.IsNullOrWhiteSpace(command.Reason)) return Result<DocumentResponse>.Failure(DocFlow.Application.ApplicationErrors.RetryReasonRequired);

        var retry = document.Retry(command.Reason, _clock.UtcNow);
        if (retry.IsFailure) return Result<DocumentResponse>.Failure(retry.Error!);
        await _unitOfWork.SaveChangesAsync(ct);
        await _jobs.EnqueueDocumentProcessingAsync(document.Id, ct);
        return Result<DocumentResponse>.Success(DocumentMapper.ToResponse(document));
    }

    public async Task<Result<DocumentResponse>> CancelAsync(Guid documentId, CancelDocumentCommand command, CancellationToken ct = default)
    {
        var document = await _documents.GetByIdAsync(documentId, ct);
        if (document is null) return Result<DocumentResponse>.Failure(DocFlow.Application.ApplicationErrors.DocumentNotFound);
        var cancel = document.Cancel(command.Reason, _clock.UtcNow);
        if (cancel.IsFailure) return Result<DocumentResponse>.Failure(cancel.Error!);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<DocumentResponse>.Success(DocumentMapper.ToResponse(document));
    }

    public async Task<Result<IReadOnlyList<DocumentHistoryResponse>>> GetHistoryAsync(Guid documentId, CancellationToken ct = default)
    {
        var document = await _documents.GetByIdAsync(documentId, ct);
        if (document is null) return Result<IReadOnlyList<DocumentHistoryResponse>>.Failure(DocFlow.Application.ApplicationErrors.DocumentNotFound);
        var history = document.History.OrderBy(x => x.CreatedAtUtc).Select(DocumentMapper.ToHistoryResponse).ToList();
        return Result<IReadOnlyList<DocumentHistoryResponse>>.Success(history);
    }

    private static string BuildFailureReason(Exception exception)
    {
        var message = string.IsNullOrWhiteSpace(exception.Message)
            ? "Unknown processing error."
            : exception.Message.Trim();

        return message.Length <= 1000 ? message : message[..1000];
    }
}
