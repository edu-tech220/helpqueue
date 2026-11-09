using System.Net;
using System.Net.Http.Json;
using HelpQueue.Api.Contracts.Auth;
using HelpQueue.Api.Data;
using HelpQueue.Api.Tests.Infrastructure;
using Xunit;

namespace HelpQueue.Api.Tests.Api;

public class AuthApiTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public AuthApiTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwtAndRole()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "customer@helpqueue.dev", password = SeedData.DemoPassword });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Customer", body.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "customer@helpqueue.dev", password = "not-the-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@helpqueue.dev", password = SeedData.DemoPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
