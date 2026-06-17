using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DocFlow.Api.Tests;

public sealed class DocumentCancelApiTests
{
    [Fact]
    public async Task Cancel_ShouldReturn200_WhenDocumentUploaded()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        await AuthenticateAsync(client);
        var documentId = await UploadTextDocumentAsync(client);

        var cancelResponse = await client.PostAsJsonAsync($"/api/documents/{documentId}/cancel", new
        {
            reason = "Uploaded by mistake."
        });

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelJson = await cancelResponse.Content.ReadAsStringAsync();
        using var cancelledDocument = JsonDocument.Parse(cancelJson);
        cancelledDocument.RootElement.GetProperty("status").GetString().Should().Be("Cancelled");

        var historyResponse = await client.GetAsync($"/api/documents/{documentId}/history");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var historyJson = await historyResponse.Content.ReadAsStringAsync();
        historyJson.Should().Contain("Uploaded");
        historyJson.Should().Contain("Cancelled");
        historyJson.Should().Contain("Uploaded by mistake.");
    }

    private static async Task<Guid> UploadTextDocumentAsync(HttpClient client)
    {
        using var content = CreateTextFileContent("document that will be cancelled");
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
        multipart.Add(file, "file", "cancel-example.txt");
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
