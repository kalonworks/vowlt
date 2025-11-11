using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Vowlt.Api.Data;
using Vowlt.Api.Extensions.Logging;
using Vowlt.Api.Features.Auth.Models;
using Vowlt.Api.Features.Auth.Options;
using Vowlt.Api.Features.Auth.Services;
using Vowlt.Api.Features.Bookmarks.Services;
using Vowlt.Api.Features.Embedding.Options;
using Vowlt.Api.Features.Embedding.Services;
using Vowlt.Api.Features.Llm.Options;
using Vowlt.Api.Features.Llm.Services;
using Vowlt.Api.Features.Metadata.Options;
using Vowlt.Api.Features.Metadata.Services;
using Vowlt.Api.Features.Search.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVowltDatabase(
      this IServiceCollection services,
      IConfiguration configuration,
      IWebHostEnvironment environment)
    {
        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "vowlt";
        var username = Environment.GetEnvironmentVariable("POSTGRES_USER")
            ?? throw new InvalidOperationException("POSTGRES_USER is required");
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
            ?? throw new InvalidOperationException("POSTGRES_PASSWORD is required");

        // Production validation - don't allow defaults
        if (environment.IsProduction())
        {
            if (host == "localhost")
            {
                throw new InvalidOperationException(
                    "POSTGRES_HOST must be explicitly set in production (currently defaulting to 'localhost'). " +
                    "Set the POSTGRES_HOST environment variable.");
            }

            if (database == "vowlt")
            {
                throw new InvalidOperationException(
                    "POSTGRES_DB should be explicitly set in production (currently using default 'vowlt'). " +
                    "Set the POSTGRES_DB environment variable.");
            }
        }

        var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("Database");
        logger.DatabaseConfigured(host, database, username);

        services.AddDbContext<VowltDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.UseVector()));

        return services;
    }

    public static IServiceCollection AddVowltIdentity(
        this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            options.User.RequireUniqueEmail = true;

            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<VowltDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection AddVowltJwtAuthentication(
      this IServiceCollection services,
      IConfiguration configuration,
      IWebHostEnvironment environment)  // ← Add this parameter
    {
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing");

        if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
        {
            throw new InvalidOperationException("JWT Secret is required");
        }

        // Production validation
        if (environment.IsProduction())
        {
            if (jwtOptions.Issuer.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "JWT Issuer cannot contain 'localhost' in production. " +
                    "Set Jwt__Issuer to your production domain (e.g., https://api.vowlt.com).");
            }

            if (jwtOptions.Audience.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "JWT Audience cannot contain 'localhost' in production. " +
                    "Set Jwt__Audience to your production domain.");
            }

            if (jwtOptions.Secret.Contains("dev", StringComparison.OrdinalIgnoreCase) ||
                jwtOptions.Secret.Contains("development", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "JWT Secret appears to be a development value. " +
                    "Use a secure, production-grade secret in production.");
            }

            if (jwtOptions.Secret.Length < 32)
            {
                throw new InvalidOperationException(
                    $"JWT Secret must be at least 32 characters in production (currently {jwtOptions.Secret.Length} characters).");
            }
        }

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("Authentication");
        logger.JwtConfigured(jwtOptions.Issuer, jwtOptions.Audience, jwtOptions.AccessTokenExpiryMinutes);


        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        return services;
    }

    public static IServiceCollection AddVowltRateLimiting(
     this IServiceCollection services,
     IConfiguration configuration,
     IWebHostEnvironment environment)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("RateLimiting");

        var configSection = configuration.GetSection(RateLimitOptions.SectionName);
        var rateLimitOptions = new RateLimitOptions();

        // Check if configuration section exists
        if (!configSection.Exists())
        {
            if (environment.IsProduction())
            {
                throw new InvalidOperationException(
                    $"Rate limit configuration section '{RateLimitOptions.SectionName}' is required in production. " +
                    "Set RateLimits__Login__PermitLimit, RateLimits__Register__PermitLimit, " +
                    "and RateLimits__Refresh__PermitLimit in environment variables.");
            }

            logger.LogWarning(
                "Rate limit configuration section '{SectionName}' not found. Using hardcoded defaults (development only).",
                RateLimitOptions.SectionName);
        }
        else
        {
            // Bind configuration
            configSection.Bind(rateLimitOptions);

            // Validate individual settings
            var expensiveLimitConfig = configuration[$"{RateLimitOptions.SectionName}:ExpensiveOperation:PermitLimit"];
            var loginLimitConfig = configuration[$"{RateLimitOptions.SectionName}:Login:PermitLimit"];
            var registerLimitConfig = configuration[$"{RateLimitOptions.SectionName}:Register:PermitLimit"];
            var refreshLimitConfig = configuration[$"{RateLimitOptions.SectionName}:Refresh:PermitLimit"];

            // In production, require all rate limit settings
            if (environment.IsProduction())
            {
                var missingConfigs = new List<string>();

                if (string.IsNullOrEmpty(loginLimitConfig))
                    missingConfigs.Add("RateLimits__Login__PermitLimit");
                if (string.IsNullOrEmpty(registerLimitConfig))
                    missingConfigs.Add("RateLimits__Register__PermitLimit");
                if (string.IsNullOrEmpty(refreshLimitConfig))
                    missingConfigs.Add("RateLimits__Refresh__PermitLimit");
                if (string.IsNullOrEmpty(expensiveLimitConfig))
                    missingConfigs.Add("RateLimits__ExpensiveOperation__PermitLimit");


                if (missingConfigs.Any())
                {
                    throw new InvalidOperationException(
                        $"Required rate limit configuration missing in production: {string.Join(", ", missingConfigs)}. "
    +
                        "These environment variables must be set.");
                }
            }

            // Log which values are loaded vs using defaults (development only)
            if (string.IsNullOrEmpty(expensiveLimitConfig))
            {
                logger.LogWarning(
                    "Expensive operation rate limit not configured (RateLimits__ExpensiveOperation__PermitLimit missing). Using default: {Default}",
                    rateLimitOptions.ExpensiveOperation.PermitLimit);
            }
            else
            {
                logger.LogInformation(
                    "Expensive operation rate limit loaded from configuration: {Value}/{Window}",
                    rateLimitOptions.ExpensiveOperation.PermitLimit,
                    $"{rateLimitOptions.ExpensiveOperation.WindowMinutes}min");
            }

            if (string.IsNullOrEmpty(loginLimitConfig))
            {
                logger.LogWarning(
                    "Login rate limit not configured (RateLimits__Login__PermitLimit missing). Using default: {Default}",
                    rateLimitOptions.Login.PermitLimit);
            }
            else
            {
                logger.LogInformation(
                    "Login rate limit loaded from configuration: {Value}/{Window}",
                    rateLimitOptions.Login.PermitLimit,
                    $"{rateLimitOptions.Login.WindowMinutes}min");
            }

            if (string.IsNullOrEmpty(registerLimitConfig))
            {
                logger.LogWarning(
                    "Register rate limit not configured (RateLimits__Register__PermitLimit missing). Using default: { Default}",
                    rateLimitOptions.Register.PermitLimit);
            }
            else
            {
                logger.LogInformation(
                    "Register rate limit loaded from configuration: {Value}/{Window}",
                    rateLimitOptions.Register.PermitLimit,
                    $"{rateLimitOptions.Register.WindowHours}hr");
            }

            if (string.IsNullOrEmpty(refreshLimitConfig))
            {
                logger.LogWarning(
                    "Refresh rate limit not configured (RateLimits__Refresh__PermitLimit missing). Using default: { Default}",
                    rateLimitOptions.Refresh.PermitLimit);
            }
            else
            {
                logger.LogInformation(
                    "Refresh rate limit loaded from configuration: {Value}/{Window}",
                    rateLimitOptions.Refresh.PermitLimit,
                    $"{rateLimitOptions.Refresh.WindowHours}hr");
            }
        }

        // Final summary
        logger.LogInformation(
              "Rate limits configured from {Source}: Login={LoginLimit}/{LoginWindow}, Register={RegisterLimit}/{RegisterWindow}, Refresh={RefreshLimit}/{RefreshWindow}, ExpensiveOps={ExpensiveLimit}/{ExpensiveWindow}",
              configSection.Exists() ? "configuration" : "hardcoded defaults",
              rateLimitOptions.Login.PermitLimit,
              $"{rateLimitOptions.Login.WindowMinutes}min",
              rateLimitOptions.Register.PermitLimit,
              $"{rateLimitOptions.Register.WindowHours}hr",
              rateLimitOptions.Refresh.PermitLimit,
              $"{rateLimitOptions.Refresh.WindowHours}hr",
              rateLimitOptions.ExpensiveOperation.PermitLimit,
              $"{rateLimitOptions.ExpensiveOperation.WindowMinutes}min");


        services.AddRateLimiter(options =>
        {
            options.AddPolicy("login", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.Login.PermitLimit,
                        Window = TimeSpan.FromMinutes(rateLimitOptions.Login.WindowMinutes),
                        QueueLimit = environment.IsProduction() ? 0 : 2
                    }));

            options.AddPolicy("register", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.Register.PermitLimit,
                        Window = TimeSpan.FromHours(rateLimitOptions.Register.WindowHours),
                        QueueLimit = environment.IsProduction() ? 1 : 5
                    }));

            options.AddPolicy("refresh-token", context =>
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.Refresh.PermitLimit,
                        Window = TimeSpan.FromHours(rateLimitOptions.Refresh.WindowHours),
                        QueueLimit = environment.IsProduction() ? 5 : 10
                    });
            });

            options.AddPolicy("expensive-operation", context =>
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.ExpensiveOperation.PermitLimit,
                        Window = TimeSpan.FromMinutes(rateLimitOptions.ExpensiveOperation.WindowMinutes),
                        QueueLimit = 0
                    });
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.Headers["Retry-After"] = "60";

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    message = "Too many requests. Please try again later.",
                    retryAfter = 60
                }, token);
            };
        });

        return services;
    }


    public static IServiceCollection AddVowltValidation(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddFluentValidationAutoValidation();

        return services;
    }

    public static IServiceCollection AddVowltCors(
         this IServiceCollection services,
         IWebHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            if (environment.IsDevelopment())
            {
                // Development: Allow all origins for easy testing
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            }
            else
            {
                // Production: Restrict to configured origins
                var allowedOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
                    ?? throw new InvalidOperationException(
                        "CORS_ALLOWED_ORIGINS environment variable is required in production. " +
                        "Set comma-separated list of allowed origins (e.g., 'https://app.vowlt.com,https://www.vowlt.com')");


                var origins = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                options.AddPolicy("AllowAll", policy =>
                {
                    policy.WithOrigins(origins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials(); // Required for cookies/auth headers
                });

                using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var logger = loggerFactory.CreateLogger("CORS");
                logger.LogInformation(
                    "CORS configured for production with {Count} allowed origins: {Origins}",
                    origins.Length,
                    string.Join(", ", origins));
            }
        });

        return services;
    }


    public static IServiceCollection AddVowltSwagger(
        this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Vowlt API",
                Version = "v1",
                Description = "Vowlt Web API with JWT Authentication"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token (Bearer prefix added automatically).\n\nExample: eyJhbGc..."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
            });
        });

        return services;
    }

    public static IServiceCollection AddVowltEmbedding(
       this IServiceCollection services,
       IConfiguration configuration,
       IWebHostEnvironment environment)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("Embedding");

        // Configure options
        services.Configure<EmbeddingOptions>(
            configuration.GetSection(EmbeddingOptions.SectionName));

        var configSection = configuration.GetSection(EmbeddingOptions.SectionName);
        var embeddingOptions = configSection.Get<EmbeddingOptions>() ?? new EmbeddingOptions();

        // Check individual config values
        var serviceUrlConfig = configuration[$"{EmbeddingOptions.SectionName}:ServiceUrl"];
        var timeoutConfig = configuration[$"{EmbeddingOptions.SectionName}:TimeoutSeconds"];
        var retriesConfig = configuration[$"{EmbeddingOptions.SectionName}:MaxRetries"];

        // Determine source
        string configSource;
        if (!configSection.Exists())
        {
            // No configuration section at all
            if (environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "Embedding service configuration is required in production. " +
                    "Set Embedding__ServiceUrl environment variable.");
            }

            logger.LogWarning(
                "Embedding configuration section '{SectionName}' not found. Using hardcoded defaults (development only).",
                EmbeddingOptions.SectionName);

            configSource = "hardcoded defaults";
        }
        else
        {
            // Log individual values
            if (string.IsNullOrEmpty(serviceUrlConfig))
            {
                logger.LogWarning(
                    "Embedding service URL not configured (Embedding__ServiceUrl missing). Using default: {Default}",
                    embeddingOptions.ServiceUrl);
            }
            else
            {
                logger.LogInformation(
                    "Embedding service URL loaded from configuration: {Url}",
                    embeddingOptions.ServiceUrl);
            }

            if (string.IsNullOrEmpty(timeoutConfig))
            {
                logger.LogWarning(
                    "Embedding timeout not configured (Embedding__TimeoutSeconds missing). Using default: {Default}s",
                    embeddingOptions.TimeoutSeconds);
            }

            if (string.IsNullOrEmpty(retriesConfig))
            {
                logger.LogWarning(
                    "Embedding max retries not configured (Embedding__MaxRetries missing). Using default: {Default}",
                    embeddingOptions.MaxRetries);
            }

            // Production validation
            if (environment.IsProduction())
            {
                var missingConfigs = new List<string>();

                if (string.IsNullOrEmpty(serviceUrlConfig))
                    missingConfigs.Add("Embedding__ServiceUrl");
                if (string.IsNullOrEmpty(timeoutConfig))
                    missingConfigs.Add("Embedding__TimeoutSeconds");
                if (string.IsNullOrEmpty(retriesConfig))
                    missingConfigs.Add("Embedding__MaxRetries");

                if (missingConfigs.Any())
                {
                    throw new InvalidOperationException(
                        $"Required embedding configuration missing in production: {string.Join(", ", missingConfigs)}. " +
                        "These environment variables must be set.");
                }

                if (embeddingOptions.ServiceUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Embedding ServiceUrl cannot contain 'localhost' in production. " +
                        "Set Embedding__ServiceUrl to your service name (e.g., http://embedding:8000).");
                }
            }

            configSource = "configuration";
        }

        if (embeddingOptions.VectorDimensions != 384)
        {
            var message = $"VectorDimensions must be 384 for all-MiniLM-L6-v2 model. " +
                         $"Got {embeddingOptions.VectorDimensions}. " +
                         "Database schema is currently vector(384). " +
                         "Changing this requires migration and re-embedding all bookmarks.";

            if (environment.IsProduction())
            {
                throw new InvalidOperationException(message);
            }

            logger.LogWarning(message);
        }
        else
        {
            logger.LogInformation(
                "Vector dimensions validated: {Dimensions} (matches all-MiniLM-L6-v2 model)",
                embeddingOptions.VectorDimensions);
        }


        // Summary log
        logger.LogInformation(
            "Embedding service configured from {Source}: URL={Url}, Timeout={Timeout}s, MaxRetries={Retries}, Dimensions={Dimensions}",
            configSource,
            embeddingOptions.ServiceUrl,
            embeddingOptions.TimeoutSeconds,
            embeddingOptions.MaxRetries,
            embeddingOptions.VectorDimensions);


        // Register typed HTTP client with Polly policies
        services.AddHttpClient<IEmbeddingService, EmbeddingService>(client =>
        {
            client.BaseAddress = new Uri(embeddingOptions.ServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(embeddingOptions.TimeoutSeconds);
        })
        .AddPolicyHandler((serviceProvider, request) =>
        {
            var policyLogger = serviceProvider.GetRequiredService<ILogger<EmbeddingService>>();
            return GetRetryPolicy(embeddingOptions, policyLogger);
        })
        .AddPolicyHandler((serviceProvider, request) =>
        {
            var policyLogger = serviceProvider.GetRequiredService<ILogger<EmbeddingService>>();
            return GetCircuitBreakerPolicy(policyLogger);
        });

        return services;
    }



    // Retry policy: 3 attempts with exponential backoff
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(
        EmbeddingOptions options,
        ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // 5xx and 408
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: options.MaxRetries,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds(options.RetryDelayMs * Math.Pow(2, retryAttempt - 1)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var statusCode = outcome.Result?.StatusCode.ToString() ?? "N/A";
                    var error = outcome.Exception?.Message ?? statusCode;

                    logger.LogWarning(
                        "Embedding service retry {RetryAttempt}/{MaxRetries} after {Delay}ms. Reason: {Error}",
                        retryAttempt,
                        options.MaxRetries,
                        timespan.TotalMilliseconds,
                        error);
                });
    }

    // Circuit breaker: opens after 5 consecutive failures, half-opens after 30s
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    logger.LogError(
                        "Embedding service circuit breaker OPENED for {Duration}s after 5 consecutive failures. Last error: {Error}",
                        duration.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                },
                onReset: () =>
                {
                    logger.LogInformation(
                        "Embedding service circuit breaker RESET. Service is healthy again.");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation(
                        "Embedding service circuit breaker HALF-OPEN. Testing if service recovered.");
                });
    }

    public static IServiceCollection AddVowltBookmarks(
          this IServiceCollection services)
    {
        services.AddScoped<IBookmarkService, BookmarkService>();
        return services;
    }
    public static IServiceCollection AddVowltSearch(
      this IServiceCollection services)
    {
        services.AddScoped<ISearchService, SearchService>();
        return services;
    }
    public static IServiceCollection AddVowltLlm(
         this IServiceCollection services,
         IConfiguration configuration,
         IHostEnvironment environment)
    {
        // Create temporary logger for startup validation
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("Vowlt.Startup");

        logger.LogInformation("Configuring LLM services...");

        // Bind LLM configuration
        var llmOptions = new LlmOptions();
        configuration.GetSection(LlmOptions.SectionName).Bind(llmOptions);
        services.Configure<LlmOptions>(configuration.GetSection(LlmOptions.SectionName));

        // Log provider selection
        logger.LogInformation("Selected LLM provider: {Provider}", llmOptions.Provider);

        // Validate provider selection
        if (string.IsNullOrWhiteSpace(llmOptions.Provider))
        {
            throw new InvalidOperationException(
                "LLM provider must be specified in configuration (Llm:Provider)");
        }

        // Register appropriate provider based on selection
        switch (llmOptions.Provider.ToLowerInvariant())
        {
            case "gemini":
                AddGeminiProvider(services, configuration, environment, logger);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported LLM provider: {llmOptions.Provider}. Supported providers: Gemini");
        }

        // Register tag generation service
        services.AddScoped<ITagGenerationService, TagGenerationService>();

        logger.LogInformation("LLM services configured successfully");
        return services;
    }

    public static IServiceCollection AddVowltMetadataExtraction(
          this IServiceCollection services,
          IConfiguration configuration,
          IHostEnvironment environment)
    {
        // Create temporary logger for startup validation
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("Vowlt.Startup");

        logger.LogInformation("Configuring metadata extraction service...");

        // Bind configuration options
        var configSection = configuration.GetSection(MetadataExtractionOptions.SectionName);
        var options = configSection.Get<MetadataExtractionOptions>() ?? new MetadataExtractionOptions();
        services.Configure<MetadataExtractionOptions>(configSection);

        // Log configuration details
        logger.LogInformation(
            "Metadata extraction timeout loaded from configuration: {Timeout}s",
            options.TimeoutSeconds);
        logger.LogInformation(
            "Metadata extraction max retries loaded from configuration: {MaxRetries}",
            options.MaxRetries);
        logger.LogInformation(
            "Metadata extraction user agent: {UserAgent}",
            options.UserAgent.Substring(0, Math.Min(50, options.UserAgent.Length)) + "...");

        // Summary log
        logger.LogInformation(
            "Metadata extraction service configured from configuration: " +
            "Timeout={Timeout}s, MaxRetries={MaxRetries}, FollowRedirects={FollowRedirects}",
            options.TimeoutSeconds,
            options.MaxRetries,
            options.FollowRedirects);

        // Register HTTP client with Polly policies
        services.AddHttpClient<IMetadataExtractionService, MetadataExtractionService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

            // Modern browser headers to avoid bot detection
            client.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
            client.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.Add("DNT", "1"); // Do Not Track
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = options.FollowRedirects,
            MaxAutomaticRedirections = options.MaxRedirects,
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            UseCookies = false // Don't maintain cookies for scraping
        })
        .AddPolicyHandler((serviceProvider, _) =>
        {
            var policyLogger = serviceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();

            // Retry policy: exponential backoff with jitter
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: options.MaxRetries,
                    sleepDurationProvider: retryAttempt =>
                    {
                        // Exponential backoff with jitter (prevents retry storms)
                        var baseDelay = TimeSpan.FromMilliseconds(
                            options.RetryDelayMs * Math.Pow(2, retryAttempt - 1));
                        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                        return baseDelay + jitter;
                    },
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        policyLogger.LogWarning(
                            "Metadata extraction retry {RetryCount}/{MaxRetries} after {Delay}ms. Reason: {Reason}",
                            retryCount,
                            options.MaxRetries,
                            timespan.TotalMilliseconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
                    });
        })
        .AddPolicyHandler((serviceProvider, _) =>
        {
            var policyLogger = serviceProvider.GetRequiredService<ILogger<MetadataExtractionService>>();

            // Circuit breaker: open after 5 consecutive failures
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (outcome, duration) =>
                    {
                        policyLogger.LogError(
                            "Metadata extraction circuit breaker OPENED for {Duration}s. Reason: {Reason}",
                            duration.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
                    },
                    onReset: () =>
                    {
                        policyLogger.LogInformation(
                            "Metadata extraction circuit breaker RESET - service recovered");
                    });
        });

        logger.LogInformation("Metadata extraction service configured successfully");
        return services;
    }

    private static void AddGeminiProvider(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger)
    {
        logger.LogInformation("Configuring Gemini LLM provider...");

        // Bind Gemini-specific options
        var geminiOptions = new GeminiOptions();
        configuration.GetSection($"{LlmOptions.SectionName}:Gemini").Bind(geminiOptions);
        services.Configure<GeminiOptions>(configuration.GetSection($"{LlmOptions.SectionName}:Gemini"));

        // Get API key from environment variable
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        // Environment-aware validation
        if (environment.IsProduction())
        {
            // Production: Strict validation
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "GEMINI_API_KEY environment variable is required in production");
            }

            if (string.IsNullOrWhiteSpace(geminiOptions.BaseUrl))
            {
                throw new InvalidOperationException(
                    "Gemini BaseUrl must be specified in production");
            }

            if (geminiOptions.BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Gemini BaseUrl cannot be localhost in production");
            }

            logger.LogInformation("Gemini API key: Loaded from environment ✓");
        }
        else
        {
            // Development: Warnings only
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning(
                    "GEMINI_API_KEY not set. AI tag generation will fail. " +
                    "Set the environment variable or add to .env file");
            }
            else
            {
                logger.LogInformation("Gemini API key: Loaded from environment ✓");
            }
        }

        // Log configuration details
        logger.LogInformation("Gemini configuration:");
        logger.LogInformation("  Model: {Model}", geminiOptions.Model);
        logger.LogInformation("  BaseUrl: {BaseUrl}", geminiOptions.BaseUrl);
        logger.LogInformation("  Timeout: {Timeout}s", geminiOptions.TimeoutSeconds);

        // Register Gemini LLM service with HTTP client
        services.AddHttpClient<ILlmService, GeminiLlmService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<GeminiOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

            // Add Gemini API key header if available
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
            }
        })
        .AddPolicyHandler((serviceProvider, _) =>
        {
            var policyLogger = serviceProvider.GetRequiredService<ILogger<GeminiLlmService>>();
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        policyLogger.LogWarning(
                            "Gemini API retry {RetryCount} after {Delay}s. Reason: {Reason}",
                            retryCount,
                            timespan.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
                    });
        })
        .AddPolicyHandler((serviceProvider, _) =>
        {
            var policyLogger = serviceProvider.GetRequiredService<ILogger<GeminiLlmService>>();
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (outcome, duration) =>
                    {
                        policyLogger.LogError(
                            "Gemini API circuit breaker opened for {Duration}s. Reason: {Reason}",
                            duration.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
                    },
                    onReset: () =>
                    {
                        policyLogger.LogInformation("Gemini API circuit breaker reset - service recovered");
                    });
        });

        logger.LogInformation("Gemini provider configured successfully");
    }
}

