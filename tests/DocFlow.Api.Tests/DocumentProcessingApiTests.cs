using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DocFlow.Api.Tests;

public sealed class DocumentProcessingApiTests
{
    [Fact]
    public async Task Process_ShouldReturn200_AndMarkDocumentProcessed()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        await AuthenticateAsync(client);
        var documentId = await UploadTextDocumentAsync(client);

        var processResponse = await client.PostAsync($"/api/documents/{documentId}/process", content: null);

        processResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var processBody = await processResponse.Content.ReadAsStringAsync();
        processBody.Should().Contain("Processed");
        processBody.Should().Contain("Fake extracted text preview");

        var getResponse = await client.GetAsync($"/api/documents/{documentId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getBody = await getResponse.Content.ReadAsStringAsync();
        getBody.Should().Contain("Processed");
    }

    [Fact]
    public async Task History_ShouldReturnStatusTransitions_AfterProcessing()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        await AuthenticateAsync(client);
        var documentId = await UploadTextDocumentAsync(client);

        var processResponse = await client.PostAsync($"/api/documents/{documentId}/process", content: null);
        processResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var historyResponse = await client.GetAsync($"/api/documents/{documentId}/history");

        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var historyBody = await historyResponse.Content.ReadAsStringAsync();
        historyBody.Should().Contain("Uploaded");
        historyBody.Should().Contain("Queued");
        historyBody.Should().Contain("Processing");
        historyBody.Should().Contain("Processed");
    }

    private static async Task<Guid> UploadTextDocumentAsync(HttpClient client)
    {
        using var content = CreateTextFileContent("hello processing workflow");
        var response = await client.PostAsync("/api/documents", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("id").GetGuid();
    }

    private static MultipartFormDataContent CreateTextFileContent(string text)
    {
        var multipart = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes(text);
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        multipart.Add(file, "file", "processing-example.txt");
        return multipart;
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = "operator",
            password = "Operator123!"
        });

        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginBody>();
        body.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
    }

    private sealed record LoginBody(string AccessToken, string TokenType, DateTime ExpiresAtUtc, string UserName, string Role);
}
