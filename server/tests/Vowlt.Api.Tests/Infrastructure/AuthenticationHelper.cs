using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Vowlt.Api.Features.Auth.DTOs;
using Vowlt.Api.Features.Auth.Models;

namespace Vowlt.Api.Tests.Infrastructure;

/// <summary>
/// Helper methods for creating authenticated test users and getting JWT tokens.
/// </summary>
public static class AuthenticationHelper
{
    /// <summary>
    /// Creates a test user directly in the database.
    /// </summary>
    public static async Task<ApplicationUser> CreateTestUserAsync(
        IServiceProvider services,
        string email = "test@example.com",
        string password = "Test123!@#")
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Test User"
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    /// <summary>
    /// Gets a JWT access token by calling the real login endpoint.
    /// </summary>
    public static async Task<string> GetAccessTokenAsync(
        HttpClient client,
        IServiceProvider services,
        string email = "test@example.com",
        string password = "Test123!@#")
    {
        // Create user
        await CreateTestUserAsync(services, email, password);

        // Login via API
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

        if (authResponse?.AccessToken == null)
        {
            throw new InvalidOperationException("Login failed - no access token");
        }

        return authResponse.AccessToken;
    }

    /// <summary>
    /// Creates an authenticated HttpClient with Bearer token already set.
    /// </summary>
    public static async Task<HttpClient> GetAuthenticatedClientAsync(
        VowltWebApplicationFactory factory,
        string email = "test@example.com",
        string password = "Test123!@#")
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var token = await GetAccessTokenAsync(client, services, email, password);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}
