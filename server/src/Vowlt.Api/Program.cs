using Vowlt.Api.Extensions.Logging;

var projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../"));
var envPath = Path.Combine(projectRoot, ".env");

// Create a temporary logger for startup
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var startupLogger = loggerFactory.CreateLogger("Startup");

if (File.Exists(envPath))
{
    startupLogger.EnvironmentFileLoaded(envPath);
    DotNetEnv.Env.Load(envPath);
}
else
{
    startupLogger.EnvironmentFileNotFound(envPath);
}


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVowltDatabase(builder.Configuration, builder.Environment);
builder.Services.AddVowltIdentity();
builder.Services.AddVowltOAuth();
builder.Services.AddVowltJwtAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddVowltRateLimiting(builder.Configuration, builder.Environment);
builder.Services.AddVowltValidation();
builder.Services.AddVowltCors(builder.Environment);
builder.Services.AddVowltSwagger();
builder.Services.AddVowltEmbedding(builder.Configuration, builder.Environment);
builder.Services.AddVowltBookmarks();
builder.Services.AddVowltMetadataExtraction(builder.Configuration, builder.Environment);
builder.Services.AddVowltLlm(builder.Configuration, builder.Environment);
builder.Services.AddVowltSearch();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Test"))
{
    await app.SeedDatabaseAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseVowltSwagger();
}

app.UseVowltCors();
app.UseVowltAuthentication();
app.MapControllers();

app.Run();

public partial class Program { }

