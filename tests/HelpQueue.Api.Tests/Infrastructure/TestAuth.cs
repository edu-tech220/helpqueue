using System.Net.Http.Headers;
using System.Net.Http.Json;
using HelpQueue.Api.Contracts.Auth;
using HelpQueue.Api.Data;

namespace HelpQueue.Api.Tests.Infrastructure;

public static class TestAuth
{
    public static async Task<HttpClient> LoginAsAsync(this ApiTestFactory factory, string email, string password = SeedData.DemoPassword)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }
}
