using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Vowlt.Api.Data;
using Vowlt.Api.Features.Auth.Models;
using Vowlt.Api.Features.Auth.Options;
using Vowlt.Api.Features.Auth.Services;

//env
var projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../"));
var envPath = Path.Combine(projectRoot, ".env");

if (File.Exists(envPath))
{
    Console.WriteLine($"Loading .env from: {envPath}");
    DotNetEnv.Env.Load(envPath);
}
else
{
    Console.WriteLine($".env file not found at: {envPath}");
    Console.WriteLine("Using environment variables from system");
}


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// db
var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "vowlt";
var username = Environment.GetEnvironmentVariable("POSTGRES_USER")
    ?? throw new InvalidOperationException("POSTGRES_USER is required");
var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
    ?? throw new InvalidOperationException("POSTGRES_PASSWORD is required");

var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Include Error Detail=true";

Console.WriteLine($"Connecting to: Host={host}, Database={database}, User={username}");

builder.Services.AddDbContext<VowltDbContext>(options =>
      options.UseNpgsql(connectionString, npgsqlOptions =>
          npgsqlOptions.UseVector()));


// identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
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

// jwt 
builder.Services.Configure<JwtOptions>(
      builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing");

if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
{
    throw new InvalidOperationException("JWT Secret is required");
}

builder.Services
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

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
  {
      options.AddFixedWindowLimiter("refresh", opt =>
      {
          opt.PermitLimit = 10;  // 10 refresh attempts
          opt.Window = TimeSpan.FromMinutes(1);  // per minute
          opt.QueueLimit = 0;
      });
  });


builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<RefreshTokenService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddCors(options =>
  {
      options.AddPolicy("AllowAll", policy =>
      {
          policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
      });
  });


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
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
        Description = "Enter your JWT token in the field below. The 'Bearer' prefix will be added automatically.\n\nExample: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

//app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
