using Microsoft.Extensions.DependencyInjection;

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

builder.Services.AddVowltDatabase(builder.Configuration);
builder.Services.AddVowltIdentity();
builder.Services.AddVowltJwtAuthentication(builder.Configuration);
builder.Services.AddVowltRateLimiting();
builder.Services.AddVowltValidation();
builder.Services.AddVowltCors();
builder.Services.AddVowltSwagger();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseVowltSwagger();
}

app.UseVowltCors();
app.UseVowltAuthentication();
app.MapControllers();

app.Run();
