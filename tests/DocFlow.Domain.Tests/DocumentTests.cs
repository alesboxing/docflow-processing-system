using DocFlow.Domain.Documents;
using FluentAssertions;

namespace DocFlow.Domain.Tests;

public sealed class DocumentTests
{
    [Fact]
    public void Create_ShouldCreateUploadedDocument_WhenValid()
    {
        var result = Document.Create("a.txt", "stored.txt", "text/plain", 10, "checksum", DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(DocumentStatus.Uploaded);
        result.Value.History.Should().ContainSingle();
    }

    [Fact]
    public void StartProcessing_ShouldFail_WhenDocumentIsUploaded()
    {
        var document = Document.Create("a.txt", "stored.txt", "text/plain", 10, "checksum", DateTime.UtcNow).Value;

        var result = document.StartProcessing(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        document.Status.Should().Be(DocumentStatus.Uploaded);
    }

    [Fact]
    public void MarkProcessed_ShouldMoveProcessingToProcessed()
    {
        var document = Document.Create("a.txt", "stored.txt", "text/plain", 10, "checksum", DateTime.UtcNow).Value;
        document.MarkQueued(DateTime.UtcNow);
        document.StartProcessing(DateTime.UtcNow);
        var processingResult = ProcessingResult.Create("title", "preview", 1, null).Value;

        var result = document.MarkProcessed(processingResult, DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        document.Status.Should().Be(DocumentStatus.Processed);
        document.ExtractedTitle.Should().Be("title");
    }
}
