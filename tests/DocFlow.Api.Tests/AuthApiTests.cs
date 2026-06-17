using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace DocFlow.Api.Tests;

public sealed class AuthApiTests
{
    [Fact]
    public async Task Health_ShouldReturnOk()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ShouldReturn200_WhenCredentialsValid()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = "operator",
            password = "Operator123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("accessToken");
        json.Should().Contain("Operator");
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenCredentialsInvalid()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = "operator",
            password = "wrong"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_ShouldReturn200_WhenAuthenticated()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = "operator",
            password = "Operator123!"
        });

        login.EnsureSuccessStatusCode();
        var loginBody = await login.Content.ReadFromJsonAsync<LoginBody>();
        loginBody.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Documents_ShouldReturn401_WhenNoToken()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/documents");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record LoginBody(string AccessToken, string TokenType, DateTime ExpiresAtUtc, string UserName, string Role);
}
