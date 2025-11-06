using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vowlt.Api.Data;

public class VowltDbContextFactory : IDesignTimeDbContextFactory<VowltDbContext>
{
    public VowltDbContext CreateDbContext(string[] args)
    {
        DotNetEnv.Env.Load("../../../.env");

        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "vowlt";
        var username = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "vowlt_user";
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
            ?? throw new InvalidOperationException("POSTGRES_PASSWORD not found");

        var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";

        var optionsBuilder = new DbContextOptionsBuilder<VowltDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions.UseVector()
        );

        return new VowltDbContext(optionsBuilder.Options);
    }
}
