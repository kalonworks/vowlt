if (File.Exists(".env"))
{
    DotNetEnv.Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Build connection string from environment variables
var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "vowlt";
var username = Environment.GetEnvironmentVariable("POSTGRES_USER")
    ?? throw new InvalidOperationException("POSTGRES_USER is required");
var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
    ?? throw new InvalidOperationException("POSTGRES_PASSWORD is required");

var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Include Error Detail=true";

Console.WriteLine($"Connecting to: Host={host}, Database={database}, User={username}");



builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
