using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PatientManagement.Api.Data;
using PatientManagement.Api.Models.Dtos;
using PatientManagement.Api.Models.Entities;
using PatientManagement.Api.Services;
using PatientManagement.Api.Tests.TestHelpers;

namespace PatientManagement.Api.Tests.Services;

public sealed class PatientServiceTests : IDisposable
{
    private const string UploadedPhotoUrl = "https://account.blob.core.windows.net/patient-photos/new.png";
    private const string OldPhotoUrl = "https://account.blob.core.windows.net/patient-photos/old.png";

    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly PatientService _service;

    public PatientServiceTests()
    {
        // SQLite in-memory lives only while the connection is open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _blobStorage
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(UploadedPhotoUrl);

        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(Now);

        _service = new PatientService(
            _context, _blobStorage, _emailService, timeProvider, NullLogger<PatientService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SavesPatientWithPhotoUrl()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        var response = await _service.CreateAsync(request, CancellationToken.None);

        // Assert
        var saved = await _context.Patients.AsNoTracking().SingleAsync();
        Assert.Equal(saved.Id, response.Id);
        Assert.Equal("Jane Doe", saved.Name);
        Assert.Equal(UploadedPhotoUrl, saved.PhotoUrl);
        Assert.Equal(Now.UtcDateTime, saved.CreatedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_UploadsPhotoAndSendsEmail()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        await _service.CreateAsync(request, CancellationToken.None);

        // Assert
        await _blobStorage.Received(1).UploadAsync(
            Arg.Any<Stream>(), "image/png", ".png", Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendPatientCreatedAsync(
            "Jane Doe", "jane@example.com", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_EmailFails_StillReturnsCreatedPatient()
    {
        // Arrange
        _emailService
            .SendPatientCreatedAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("mail server down"));

        // Act
        var response = await _service.CreateAsync(CreateRequest(), CancellationToken.None);

        // Assert
        Assert.True(response.Id > 0);
        Assert.Equal(1, await _context.Patients.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_SaveFails_DeletesUploadedPhotoAndRethrows()
    {
        // Arrange
        // A null name breaks the NOT NULL constraint, which makes SaveChanges fail after the upload.
        var request = new CreatePatientRequest { Name = null!, Age = 30, Gender = Gender.Female, Photo = FormFiles.Create() };

        // Act
        var exception = await Record.ExceptionAsync(() => _service.CreateAsync(request, CancellationToken.None));

        // Assert
        Assert.IsType<DbUpdateException>(exception);
        await _blobStorage.Received(1).DeleteAsync(UploadedPhotoUrl, Arg.Any<CancellationToken>());
        await _emailService.DidNotReceiveWithAnyArgs().SendPatientCreatedAsync(default!, default, default);
    }

    [Fact]
    public async Task GetPagedAsync_SecondPage_ReturnsRemainingPatientsAndTotal()
    {
        // Arrange
        await SeedPatientsAsync(count: 3);

        // Act
        var page = await _service.GetPagedAsync(page: 2, pageSize: 2, CancellationToken.None);

        // Assert
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.PageSize);
        var item = Assert.Single(page.Items);
        Assert.Equal("Patient 3", item.Name);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        // Act
        var patient = await _service.GetByIdAsync(999, CancellationToken.None);

        // Assert
        Assert.Null(patient);
    }

    [Fact]
    public async Task GetByIdAsync_PatientWithConsultation_ReturnsConsultations()
    {
        // Arrange
        var patient = new Patient { Name = "Jane Doe", Age = 30, Gender = Gender.Female };
        patient.Consultations.Add(new Consultation { ConsultationDate = Now.UtcDateTime, Diagnosis = "Flu" });
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        // Act
        var details = await _service.GetByIdAsync(patient.Id, CancellationToken.None);

        // Assert
        var consultation = Assert.Single(details!.Consultations);
        Assert.Equal("Flu", consultation.Diagnosis);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNull()
    {
        // Act
        var response = await _service.UpdateAsync(999, UpdateRequest(photo: null), CancellationToken.None);

        // Assert
        Assert.Null(response);
        await _blobStorage.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task UpdateAsync_WithoutPhoto_UpdatesFieldsAndKeepsPhoto()
    {
        // Arrange
        var id = await SeedPatientWithPhotoAsync();

        // Act
        var response = await _service.UpdateAsync(id, UpdateRequest(photo: null), CancellationToken.None);

        // Assert
        Assert.Equal("Jane Smith", response!.Name);
        Assert.Equal(31, response.Age);
        Assert.Equal(OldPhotoUrl, response.PhotoUrl);
        await _blobStorage.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }

    [Fact]
    public async Task UpdateAsync_WithNewPhoto_SavesNewUrlAndDeletesOldBlob()
    {
        // Arrange
        var id = await SeedPatientWithPhotoAsync();

        // Act
        var response = await _service.UpdateAsync(id, UpdateRequest(FormFiles.Create()), CancellationToken.None);

        // Assert
        Assert.Equal(UploadedPhotoUrl, response!.PhotoUrl);
        var saved = await _context.Patients.AsNoTracking().SingleAsync(p => p.Id == id);
        Assert.Equal(UploadedPhotoUrl, saved.PhotoUrl);
        await _blobStorage.Received(1).DeleteAsync(OldPhotoUrl, Arg.Any<CancellationToken>());
    }

    private static CreatePatientRequest CreateRequest() => new()
    {
        Name = "Jane Doe",
        Age = 30,
        Gender = Gender.Female,
        Email = "jane@example.com",
        Photo = FormFiles.Create()
    };

    private static UpdatePatientRequest UpdateRequest(Microsoft.AspNetCore.Http.IFormFile? photo) => new()
    {
        Name = "Jane Smith",
        Age = 31,
        Gender = Gender.Female,
        Photo = photo
    };

    private async Task<int> SeedPatientWithPhotoAsync()
    {
        var patient = new Patient { Name = "Jane Doe", Age = 30, Gender = Gender.Female, PhotoUrl = OldPhotoUrl };
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return patient.Id;
    }

    private async Task SeedPatientsAsync(int count)
    {
        for (var i = 1; i <= count; i++)
        {
            _context.Patients.Add(new Patient { Name = $"Patient {i}", Age = 20 + i, Gender = Gender.Other });
        }

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
