using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DocFlow.Api.Tests;

public sealed class DocumentDownloadApiTests
{
    [Fact]
    public async Task Download_ShouldReturnOriginalFile_WhenDocumentExists()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        await AuthenticateAsync(client);

        const string fileName = "download-example.txt";
        const string originalText = "original file content for download test";
        var documentId = await UploadTextDocumentAsync(client, fileName, originalText);

        var downloadResponse = await client.GetAsync($"/api/documents/{documentId}/download");

        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        downloadResponse.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");

        var downloadedText = await downloadResponse.Content.ReadAsStringAsync();
        downloadedText.Should().Be(originalText);

        var contentDisposition = downloadResponse.Content.Headers.ContentDisposition;
        contentDisposition.Should().NotBeNull();
        contentDisposition!.FileName?.Trim('"').Should().Be(fileName);
    }

    private static async Task<Guid> UploadTextDocumentAsync(HttpClient client, string fileName, string text)
    {
        using var content = CreateTextFileContent(fileName, text);
        var response = await client.PostAsync("/api/documents", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("id").GetGuid();
    }

    private static MultipartFormDataContent CreateTextFileContent(string fileName, string text)
    {
        var multipart = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes(text);
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        multipart.Add(file, "file", fileName);
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
