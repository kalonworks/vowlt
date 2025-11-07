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
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
