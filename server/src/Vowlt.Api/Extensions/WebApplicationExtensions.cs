using Microsoft.EntityFrameworkCore;
using Vowlt.Api.Data;
using Vowlt.Api.Data.Seeders;

namespace Microsoft.Extensions.DependencyInjection;

public static class WebApplicationExtensions
{
    public static WebApplication UseVowltSwagger(this WebApplication app)
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI();
        return app;
    }

    public static WebApplication UseVowltCors(this WebApplication app)
    {
        app.UseCors("AllowAll");
        return app;
    }

    public static WebApplication UseVowltAuthentication(this WebApplication app)
    {
        if (!app.Environment.IsEnvironment("Test"))
        {
            app.UseRateLimiter();
        }
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }


    /// <summary>
    /// Applies database migrations and seeds initial data on application startup.
    /// </summary>
    public static async Task<WebApplication> SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<VowltDbContext>();
            var environment = services.GetRequiredService<IWebHostEnvironment>();
            var timeProvider = services.GetRequiredService<TimeProvider>();

            logger.LogInformation("Starting database migration and seeding...");

            // Ensure database exists and all migrations are applied
            await context.Database.MigrateAsync();
            logger.LogInformation("✓ Database migrations applied successfully");

            // Seed OAuth clients
            await OAuthClientSeeder.SeedAsync(context, environment, timeProvider, logger);

            logger.LogInformation("✓ Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ An error occurred during database migration or seeding");

            // Fail-fast in production (don't start with corrupted data)
            if (app.Environment.IsProduction())
            {
                logger.LogCritical("Database seeding failed in production. Application will not start.");
                throw;
            }

            // In development, log but continue (allows debugging)
            logger.LogWarning("Database seeding failed in development. Application will start anyway.");
        }

        return app;
    }

}
