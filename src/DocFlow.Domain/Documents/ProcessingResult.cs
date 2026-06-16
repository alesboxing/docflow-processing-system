using DocFlow.Domain.Shared;

namespace DocFlow.Domain.Documents;

public sealed record ProcessingResult(string Title, string TextPreview, int PageCount, string? MetadataJson)
{
    public static Result<ProcessingResult> Create(string title, string textPreview, int pageCount, string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(title)) return Result<ProcessingResult>.Failure(DocumentErrors.InvalidProcessingResult);
        if (string.IsNullOrWhiteSpace(textPreview)) return Result<ProcessingResult>.Failure(DocumentErrors.InvalidProcessingResult);
        if (pageCount <= 0) return Result<ProcessingResult>.Failure(DocumentErrors.InvalidProcessingResult);
        return Result<ProcessingResult>.Success(new ProcessingResult(title.Trim(), textPreview.Trim(), pageCount, metadataJson));
    }
}
