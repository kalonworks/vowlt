using Vowlt.Api.Features.OAuth.Models;

using Microsoft.EntityFrameworkCore;

namespace Vowlt.Api.Data.Seeders;

/// <summary>
/// Seeds OAuth client configurations on application startup.
/// </summary>
public static class OAuthClientSeeder
{
    /// <summary>
    /// Seeds OAuth clients if they don't already exist.
    /// Idempotent - safe to call multiple times.
    /// </summary>
    public static async Task SeedAsync(
        VowltDbContext context,
        IWebHostEnvironment environment,
        TimeProvider timeProvider,
        ILogger logger)
    {
        // Check if OAuth clients already exist (idempotency check)
        var existingCount = await context.OAuthClients.CountAsync();
        if (existingCount > 0)
        {
            logger.LogInformation(
                "OAuth clients already seeded ({Count} clients found). Skipping.",
                existingCount);
            return;
        }

        logger.LogInformation("Seeding OAuth clients...");

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Development-only client for testing
        if (environment.IsDevelopment() || environment.IsStaging() || environment.IsEnvironment("Test"))
        {
            var devClient = OAuthClient.Create(
                clientId: "vowlt-dev-client",
                name: "Vowlt Development Client",
                description: "Local development and testing OAuth flow",
                allowedRedirectUris: "http://localhost:*,http://127.0.0.1:*,https://oauth.pstmn.io/v1/callback",
                accessTokenLifetimeMinutes: 15,  // Longer for dev convenience
                refreshTokenLifetimeDays: 7,     // Weekly re-auth for dev
                now: now
            );

            context.OAuthClients.Add(devClient);
            logger.LogInformation("Added development OAuth client");

            // SPA client (dev/test only)
            var spaClient = OAuthClient.Create(
                clientId: "vowlt-spa",
                name: "Vowlt Web Application (Development)",
                description: "React SPA for local development",
                allowedRedirectUris: "http://localhost:3000/auth/callback,http://127.0.0.1:3000/auth/callback",
                accessTokenLifetimeMinutes: 15,
                refreshTokenLifetimeDays: 7,
                now: now
            );

            context.OAuthClients.Add(spaClient);
            logger.LogInformation("Added SPA OAuth client");
        }

        // Chrome extension client (all environments)
        var chromeExtension = OAuthClient.Create(
            clientId: "vowlt-chrome-extension",
            name: "Vowlt Chrome Extension",
            description: "Official Vowlt browser extension for Chromium-based browsers",
            allowedRedirectUris: "https://*.chromiumapp.org/*",
            accessTokenLifetimeMinutes: 5,      // Very short-lived for security
            refreshTokenLifetimeDays: 30,       // Monthly re-auth
            now: now
        );

        context.OAuthClients.Add(chromeExtension);

        // Production SPA client
        if (environment.IsProduction())
        {
            var prodSpaClient = OAuthClient.Create(
                clientId: "vowlt-spa-production",
                name: "Vowlt Web Application",
                description: "React SPA for production",
                allowedRedirectUris: "https://vowlt.com/auth/callback,https://app.vowlt.com/auth/callback",
                accessTokenLifetimeMinutes: 15,
                refreshTokenLifetimeDays: 30,
                now: now
            );

            context.OAuthClients.Add(prodSpaClient);
            logger.LogInformation("Added production SPA OAuth client");
        }

        await context.SaveChangesAsync();

        var seededCount = environment.IsDevelopment() || environment.IsEnvironment("Test") ? 3 :
                         environment.IsProduction() ? 2 : 1;
        logger.LogInformation("✓ OAuth clients seeded successfully: {Count} clients added", seededCount);
    }
}
