using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using PatientManagement.Api.Data;
using PatientManagement.Api.Services;

namespace PatientManagement.Api.Tests.Integration;

/// <summary>
/// Runs the real API in memory, with SQLite in place of SQL Server and fakes for Blob Storage and email,
/// so integration tests never touch Azure.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string FakePhotoUrl = "https://fake.blob.core.windows.net/patient-photos/photo.png";

    // SQLite in-memory lives only while the connection is open, so one connection is kept for the factory's lifetime.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public IBlobStorageService BlobStorage { get; } = Substitute.For<IBlobStorageService>();

    public IEmailService EmailService { get; } = Substitute.For<IEmailService>();

    public CustomWebApplicationFactory()
    {
        BlobStorage
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(FakePhotoUrl);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureTestServices(services =>
        {
            // EF Core 10 keeps the provider setup in both registrations; leaving either one
            // causes "multiple database providers registered" once SQLite is added.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton(BlobStorage);

            services.RemoveAll<IEmailService>();
            services.AddSingleton(EmailService);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
