
  using Microsoft.AspNetCore.Hosting;
  using Microsoft.AspNetCore.Mvc.Testing;
  using Microsoft.Data.Sqlite;
  using Microsoft.EntityFrameworkCore;
  using Microsoft.EntityFrameworkCore.Infrastructure;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
  using Microsoft.Extensions.Logging;
  using Vowlt.Api.Data;
  using Vowlt.Api.Data.Seeders;

  namespace Vowlt.Api.Tests.Infrastructure;

  /// <summary>
  /// Test server factory using SQLite in-memory database.
  /// Each instance gets a unique database for test isolation.
  /// </summary>
  public class VowltWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
  {
      private readonly SqliteConnection _connection;
      private readonly string _databaseName;

      public VowltWebApplicationFactory()
      {
          // Unique database name per factory instance
          _databaseName = $"TestDb_{Guid.NewGuid()}";

          // Set environment variables
          Environment.SetEnvironmentVariable("POSTGRES_USER", "test_user");
          Environment.SetEnvironmentVariable("POSTGRES_PASSWORD", "test_password");
          Environment.SetEnvironmentVariable("POSTGRES_DB", "test_db");
          Environment.SetEnvironmentVariable("POSTGRES_HOST", "localhost");
          Environment.SetEnvironmentVariable("POSTGRES_PORT", "5432");

          Environment.SetEnvironmentVariable("Jwt__Secret", "test-secret-minimum-32-chars-for-hmacsha256-testing");
          Environment.SetEnvironmentVariable("Jwt__Issuer", "http://test-issuer");
          Environment.SetEnvironmentVariable("Jwt__Audience", "http://test-audience");
          Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "15");
          Environment.SetEnvironmentVariable("Jwt__RefreshTokenExpiryDays", "7");

          // Create and open connection (keeps in-memory DB alive)
          _connection = new SqliteConnection("DataSource=:memory:");
          _connection.Open();
      }

      protected override void ConfigureWebHost(IWebHostBuilder builder)
      {
          builder.UseEnvironment("Test");

          builder.ConfigureServices(services =>
          {
              // EF Core 9 way: Remove the configuration, not the options
              services.RemoveAll<IDbContextOptionsConfiguration<VowltDbContext>>();

              // Add SQLite
              services.AddDbContext<VowltDbContext>(options =>
              {
                  options.UseSqlite(_connection);
                  options.ReplaceService<IModelCustomizer, TestModelCustomizer>();
                  options.ConfigureWarnings(warnings =>
                  {
                      warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
                  });
              });

              // Seed
              var sp = services.BuildServiceProvider();
              using var scope = sp.CreateScope();
              var context = scope.ServiceProvider.GetRequiredService<VowltDbContext>();
              var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
              var logger = scope.ServiceProvider.GetRequiredService<ILogger<VowltWebApplicationFactory>>();

              // Create test environment
              var testEnv = new TestWebHostEnvironment { EnvironmentName = "Test" };

              context.Database.EnsureCreated();

              OAuthClientSeeder.SeedAsync(context, testEnv, timeProvider, logger)
                  .GetAwaiter()
                  .GetResult();
          });

          builder.ConfigureLogging(logging =>
          {
              logging.ClearProviders();
              logging.AddConsole();
              logging.SetMinimumLevel(LogLevel.Warning);
          });
      }

      public new void Dispose()
      {
          _connection?.Close();
          _connection?.Dispose();
          base.Dispose();
          GC.SuppressFinalize(this);
      }
  }

  public class TestModelCustomizer : RelationalModelCustomizer
  {
      public TestModelCustomizer(ModelCustomizerDependencies dependencies)
          : base(dependencies)
      {
      }

      public override void Customize(ModelBuilder modelBuilder, DbContext context)
      {
          base.Customize(modelBuilder, context);

          // Ignore Vector properties (pgvector not supported in SQLite)
          modelBuilder.Entity<Vowlt.Api.Features.Bookmarks.Models.Bookmark>()
              .Ignore(b => b.Embedding);
      }
  }

  // Minimal test environment implementation
  public class TestWebHostEnvironment : IWebHostEnvironment
  {
      public string EnvironmentName { get; set; } = "Test";
      public string ApplicationName { get; set; } = "Vowlt.Api.Tests";
      public string WebRootPath { get; set; } = string.Empty;
      public IFileProvider WebRootFileProvider { get; set; } = null!;
      public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
      public IFileProvider ContentRootFileProvider { get; set; } = null!;
  }

