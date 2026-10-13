using System.Reflection;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

using PatientManagement.Api.Options;
using PatientManagement.Api.Services;

namespace PatientManagement.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStorageAndEmailServices(this IServiceCollection services)
    {
        // Validated when first resolved (not at startup), so the app and tests can boot without blob config.
        services.AddOptions<BlobStorageOptions>()
            .BindConfiguration(BlobStorageOptions.SectionName)
            .ValidateDataAnnotations();

        services.AddOptions<EmailOptions>()
            .BindConfiguration(EmailOptions.SectionName);

        // BlobServiceClient is thread-safe and meant to be reused, so one instance for the app.
        services.AddSingleton(sp =>
            new BlobServiceClient(sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value.ConnectionString));

        services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
        services.AddScoped<IEmailService, LoggingEmailService>();

        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Patient Management API",
                Version = "v1",
                Description = "Manage patients (with a picture stored in Azure Blob Storage) and their consultations."
            });

            // The /// comments on controllers and DTOs become the descriptions shown in Swagger UI.
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
        });

        return services;
    }
}
