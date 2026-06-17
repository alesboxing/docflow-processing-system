using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DocFlow.Api.Tests;

public sealed class DocumentUploadApiTests
{
    [Fact]
    public async Task Upload_ShouldReturn201_WhenTxtFileValid()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        await AuthenticateAsync(client);

        using var content = CreateTextFileContent("hello document");

        var response = await client.PostAsync("/api/documents", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("example.txt");
        body.Should().Contain("Queued");
    }

    [Fact]
    public async Task GetById_ShouldReturn200_AfterUpload()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        await AuthenticateAsync(client);

        using var content = CreateTextFileContent("hello document");

        var uploadResponse = await client.PostAsync("/api/documents", content);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var uploadJson = await uploadResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(uploadJson);
        var id = document.RootElement.GetProperty("id").GetGuid();

        var getResponse = await client.GetAsync($"/api/documents/{id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getBody = await getResponse.Content.ReadAsStringAsync();
        getBody.Should().Contain("example.txt");
    }

    private static MultipartFormDataContent CreateTextFileContent(string text)
    {
        var multipart = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes(text);
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        multipart.Add(file, "file", "example.txt");
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
