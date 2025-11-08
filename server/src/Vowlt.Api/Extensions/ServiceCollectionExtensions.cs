using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Vowlt.Api.Data;
using Vowlt.Api.Extensions.Logging;
using Vowlt.Api.Features.Auth.Models;
using Vowlt.Api.Features.Auth.Options;
using Vowlt.Api.Features.Auth.Services;
using Vowlt.Api.Features.Bookmarks.Options;
using Vowlt.Api.Features.Bookmarks.Services;
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
        services.AddScoped<RefreshTokenService>();

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

                if (missingConfigs.Any())
                {
                    throw new InvalidOperationException(
                        $"Required rate limit configuration missing in production: {string.Join(", ", missingConfigs)}. "
    +
                        "These environment variables must be set.");
                }
            }

            // Log which values are loaded vs using defaults (development only)
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
        logger.RateLimitsConfigured(
            configSection.Exists() ? "configuration" : "hardcoded defaults",
            rateLimitOptions.Login.PermitLimit,
            $"{rateLimitOptions.Login.WindowMinutes}min",
            rateLimitOptions.Register.PermitLimit,
            $"{rateLimitOptions.Register.WindowHours}hr",
            rateLimitOptions.Refresh.PermitLimit,
            $"{rateLimitOptions.Refresh.WindowHours}hr");

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
        this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
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

        // Summary log (matches rate limiting pattern)
        logger.LogInformation(
            "Embedding service configured from {Source}: URL={Url}, Timeout={Timeout}s, MaxRetries={Retries}",
            configSource,
            embeddingOptions.ServiceUrl,
            embeddingOptions.TimeoutSeconds,
            embeddingOptions.MaxRetries);

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

}

