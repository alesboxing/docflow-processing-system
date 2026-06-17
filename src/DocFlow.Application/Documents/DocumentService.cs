using DocFlow.Application.Abstractions;
using DocFlow.Application.Common;
using DocFlow.Application.Documents.Upload;
using DocFlow.Application.Documents.Validation;
using DocFlow.Domain.Documents;
using DocFlow.Domain.Shared;

namespace DocFlow.Application.Documents;

public interface IDocumentService
{
    Task<Result<DocumentResponse>> UploadAsync(UploadDocumentCommand command, CancellationToken ct = default);
    Task<Result<DocumentResponse>> GetByIdAsync(Guid documentId, CancellationToken ct = default);
    Task<Result<PagedResponse<DocumentResponse>>> GetPagedAsync(PagedRequest request, DocumentStatus? status, CancellationToken ct = default);
}

public sealed class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IChecksumService _checksumService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FileValidationPolicy _fileValidationPolicy;

    public DocumentService(IDocumentRepository documentRepository, IFileStorage fileStorage, IBackgroundJobClient backgroundJobClient, IDateTimeProvider dateTimeProvider, IChecksumService checksumService, IUnitOfWork unitOfWork, FileValidationPolicy fileValidationPolicy)
    {
        _documentRepository = documentRepository;
        _fileStorage = fileStorage;
        _backgroundJobClient = backgroundJobClient;
        _dateTimeProvider = dateTimeProvider;
        _checksumService = checksumService;
        _unitOfWork = unitOfWork;
        _fileValidationPolicy = fileValidationPolicy;
    }

    public async Task<Result<DocumentResponse>> UploadAsync(UploadDocumentCommand command, CancellationToken ct = default)
    {
        var validationResult = _fileValidationPolicy.Validate(command);
        if (validationResult.IsFailure) return Result<DocumentResponse>.Failure(validationResult.Error!);

        var storedFileInfo = await _fileStorage.SaveAsync(command.Content, command.OriginalFileName, command.ContentType, ct);
        await using var storedStream = await _fileStorage.OpenReadAsync(storedFileInfo.StoredFileName, ct);
        var checksum = await _checksumService.CalculateSha256Async(storedStream, ct);
        var now = _dateTimeProvider.UtcNow;

        var createResult = Document.Create(command.OriginalFileName, storedFileInfo.StoredFileName, command.ContentType, command.SizeBytes, checksum, now);
        if (createResult.IsFailure) return Result<DocumentResponse>.Failure(createResult.Error!);

        var document = createResult.Value;
        var queueResult = document.MarkQueued(now);
        if (queueResult.IsFailure) return Result<DocumentResponse>.Failure(queueResult.Error!);

        await _documentRepository.AddAsync(document, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await _backgroundJobClient.EnqueueDocumentProcessingAsync(document.Id, ct);

        return Result<DocumentResponse>.Success(DocumentMapper.ToResponse(document));
    }

    public async Task<Result<DocumentResponse>> GetByIdAsync(Guid documentId, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) return Result<DocumentResponse>.Failure(DocFlow.Application.ApplicationErrors.InvalidDocumentId);
        var document = await _documentRepository.GetByIdAsync(documentId, ct);
        return document is null ? Result<DocumentResponse>.Failure(DocFlow.Application.ApplicationErrors.DocumentNotFound) : Result<DocumentResponse>.Success(DocumentMapper.ToResponse(document));
    }

    public async Task<Result<PagedResponse<DocumentResponse>>> GetPagedAsync(PagedRequest request, DocumentStatus? status, CancellationToken ct = default)
    {
        if (request.Page < 1) return Result<PagedResponse<DocumentResponse>>.Failure(DocFlow.Application.ApplicationErrors.InvalidPage);
        if (request.PageSize is < 1 or > 100) return Result<PagedResponse<DocumentResponse>>.Failure(DocFlow.Application.ApplicationErrors.InvalidPageSize);

        var items = await _documentRepository.GetPagedAsync(request.Skip, request.PageSize, status, ct);
        var totalCount = await _documentRepository.CountAsync(status, ct);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var response = new PagedResponse<DocumentResponse>(items.Select(DocumentMapper.ToResponse).ToList(), request.Page, request.PageSize, totalCount, totalPages);
        return Result<PagedResponse<DocumentResponse>>.Success(response);
    }
}
