using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
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
}
