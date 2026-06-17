using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DocFlow.Application.Abstractions;
using DocFlow.Application.Documents;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace DocFlow.Api.Tests;

public sealed class DocumentFailureRetryApiTests
{
    [Fact]
    public async Task ProcessingFailure_ShouldMarkDocumentFailed_AndRetryShouldQueueAgain()
    {
        await using var factory = new CustomWebApplicationFactory(services =>
        {
            services.RemoveAll<IDocumentProcessor>();
            services.AddScoped<IDocumentProcessor, ThrowingDocumentProcessor>();
        });

        var client = factory.CreateClient();
        await AuthenticateAsync(client);
        var documentId = await UploadTextDocumentAsync(client);

        var processResponse = await client.PostAsync($"/api/documents/{documentId}/process", content: null);

        processResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var processJson = await processResponse.Content.ReadAsStringAsync();
        using var failedDocument = JsonDocument.Parse(processJson);
        failedDocument.RootElement.GetProperty("status").GetString().Should().Be("Failed");
        failedDocument.RootElement.GetProperty("failureReason").GetString().Should().Be("Demo parser failed.");

        var retryResponse = await client.PostAsJsonAsync($"/api/documents/{documentId}/retry", new
        {
            reason = "Parser configuration fixed."
        });

        retryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retryJson = await retryResponse.Content.ReadAsStringAsync();
        using var retriedDocument = JsonDocument.Parse(retryJson);
        retriedDocument.RootElement.GetProperty("status").GetString().Should().Be("Queued");
        retriedDocument.RootElement.GetProperty("retryCount").GetInt32().Should().Be(1);
        retriedDocument.RootElement.GetProperty("failureReason").ValueKind.Should().Be(JsonValueKind.Null);

        var historyResponse = await client.GetAsync($"/api/documents/{documentId}/history");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var historyJson = await historyResponse.Content.ReadAsStringAsync();
        historyJson.Should().Contain("Failed");
        historyJson.Should().Contain("Document retry queued");
    }

    private static async Task<Guid> UploadTextDocumentAsync(HttpClient client)
    {
        using var content = CreateTextFileContent("document that will fail processing");
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
        multipart.Add(file, "file", "failure-example.txt");
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

    private sealed class ThrowingDocumentProcessor : IDocumentProcessor
    {
        public Task<ProcessingResult> ProcessAsync(Stream fileStream, string originalFileName, string contentType, CancellationToken ct = default)
        {
            throw new InvalidOperationException("Demo parser failed.");
        }
    }

    private sealed record LoginBody(string AccessToken, string TokenType, DateTime ExpiresAtUtc, string UserName, string Role);
}
