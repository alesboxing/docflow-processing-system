namespace DocFlow.Application.Documents.Upload;

public sealed record UploadDocumentCommand(
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    Stream Content);
