using DocFlow.Application;
using DocFlow.Application.Common;
using DocFlow.Application.Documents;
using DocFlow.Application.Documents.Download;
using DocFlow.Application.Documents.Processing;
using DocFlow.Application.Documents.Upload;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;
    private readonly IDocumentProcessingService _processing;
    private readonly IDocumentDownloadService _download;

    public DocumentsController(IDocumentService documents, IDocumentProcessingService processing, IDocumentDownloadService download)
    {
        _documents = documents;
        _processing = processing;
        _download = download;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadDocumentFormRequest request, CancellationToken ct)
    {
        if (request.File is null)
            return BadRequest(new ErrorResponse(ApplicationErrors.FileMissing.Code, ApplicationErrors.FileMissing.Message, ApplicationErrors.FileMissing.Type.ToString()));

        await using var stream = request.File.OpenReadStream();
        var command = new UploadDocumentCommand(request.File.FileName, request.File.ContentType, request.File.Length, stream);
        var result = await _documents.UploadAsync(command, ct);
        if (result.IsFailure) return ToHttp(result.Error!);
        return CreatedAtAction(nameof(GetById), new { documentId = result.Value.Id }, result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] GetDocumentsQuery query, CancellationToken ct)
    {
        var result = await _documents.GetPagedAsync(new PagedRequest(query.Page, query.PageSize), query.Status, ct);
        if (result.IsFailure) return ToHttp(result.Error!);
        return Ok(result.Value);
    }

    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> GetById(Guid documentId, CancellationToken ct)
    {
        var result = await _documents.GetByIdAsync(documentId, ct);
        if (result.IsFailure) return ToHttp(result.Error!);
        return Ok(result.Value);
    }

    [HttpPost("{documentId:guid}/process")]
    public async Task<IActionResult> Process(Guid documentId, CancellationToken ct)
    {
        var result = await _processing.ProcessAsync(documentId, ct);
        if (result.IsFailure) return ToHttp(result.Error!);
        return Ok(result.Value);
    }

    [HttpPost("{documentId:guid}/retry")]
    public async Task<IActionResult> Retry(Guid documentId, RetryDocumentRequest request, CancellationToken ct)
    {
        var result = await _processing.RetryAsync(documentId, new RetryDocumentCommand(request.Reason), ct);
        if (result.IsFailure) return ToHttp(result.Error!);
        return Ok(result.Value);
    }

    [HttpPost("{documentId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid documentId, CancelDocumentRequest request, CancellationToken ct)
    {
        var result = await _processing.CancelAsync(documentId, new CancelDocumentCommand(request.Reason), ct);
        if (result.IsFailure) return ToHttp(result.Error!);
        return Ok(result.Value);
    }

    [HttpGet("{documentId:guid}/history")]
    public async Task<IActionResult> History(Guid documentId, CancellationToken ct)
    {
        var result = await _processing.GetHistoryAsync(documentId, ct);
        if (result.IsFailure) return ToHttp(result.Error!);
        return Ok(result.Value);
    }

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid documentId, CancellationToken ct)
    {
        var result = await _download.DownloadAsync(documentId, ct);
        if (result.IsFailure) return ToHttp(result.Error!);
        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    private IActionResult ToHttp(DocFlow.Domain.Shared.AppError error)
    {
        var body = new ErrorResponse(error.Code, error.Message, error.Type.ToString());
        return error.Type switch
        {
            DocFlow.Domain.Shared.ErrorType.NotFound => NotFound(body),
            DocFlow.Domain.Shared.ErrorType.Conflict => Conflict(body),
            _ => BadRequest(body)
        };
    }
}
