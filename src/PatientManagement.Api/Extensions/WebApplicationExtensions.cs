using Microsoft.EntityFrameworkCore;

using PatientManagement.Api.Data;

namespace PatientManagement.Api.Extensions;

public static class WebApplicationExtensions
{
    public const string ApplyMigrationsOnStartupKey = "Database:ApplyMigrationsOnStartup";

    private const string SwaggerPath = "/swagger";

    /// <summary>
    /// Applies pending EF Core migrations when <c>Database:ApplyMigrationsOnStartup</c> is true.
    /// Only meant for the demo App Service deployment; a real system would migrate from the release pipeline.
    /// </summary>
    public static async Task ApplyMigrationsIfEnabledAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>(ApplyMigrationsOnStartupKey))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    /// <summary>Serves Swagger UI and sends the site root to it, so the App Service URL opens on the API docs.</summary>
    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        // Enabled in every environment, Production included: the brief asks for a public Swagger page on App Service.
        app.UseSwagger();
        app.UseSwaggerUI();
        app.MapGet("/", () => Results.Redirect(SwaggerPath)).ExcludeFromDescription();

        return app;
    }
}
