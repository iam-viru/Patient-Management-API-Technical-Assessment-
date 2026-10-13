using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using NSubstitute;

using PatientManagement.Api.Data;
using PatientManagement.Api.Models.Dtos;
using PatientManagement.Api.Models.Entities;
using PatientManagement.Api.Services;

namespace PatientManagement.Api.Tests.Services;

public sealed class ConsultationServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
    private static readonly DateTime ConsultationDate = new(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly ConsultationService _service;

    public ConsultationServiceTests()
    {
        // SQLite in-memory lives only while the connection is open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(Now);

        _service = new ConsultationService(_context, timeProvider);
    }

    [Fact]
    public async Task CreateAsync_UnknownPatient_ReturnsNull()
    {
        // Act
        var response = await _service.CreateAsync(999, Request("Flu"), CancellationToken.None);

        // Assert
        Assert.Null(response);
        Assert.Equal(0, await _context.Consultations.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_ExistingPatient_SavesConsultation()
    {
        // Arrange
        var patientId = await SeedPatientAsync();

        // Act
        var response = await _service.CreateAsync(patientId, Request("Flu"), CancellationToken.None);

        // Assert
        var saved = await _context.Consultations.AsNoTracking().SingleAsync();
        Assert.Equal(saved.Id, response!.Id);
        Assert.Equal(patientId, saved.PatientId);
        Assert.Equal(ConsultationDate, saved.ConsultationDate);
        Assert.Equal("Flu", saved.Diagnosis);
        Assert.Equal(Now.UtcDateTime, saved.CreatedAtUtc);
    }

    [Fact]
    public async Task GetForPatientAsync_UnknownPatient_ReturnsNull()
    {
        // Act
        var consultations = await _service.GetForPatientAsync(999, CancellationToken.None);

        // Assert
        Assert.Null(consultations);
    }

    [Fact]
    public async Task GetForPatientAsync_SeveralConsultations_ReturnsOldestFirst()
    {
        // Arrange
        var patientId = await SeedPatientAsync();
        await _service.CreateAsync(patientId, Request("Later", ConsultationDate.AddDays(5)), CancellationToken.None);
        await _service.CreateAsync(patientId, Request("Earlier", ConsultationDate), CancellationToken.None);

        // Act
        var consultations = await _service.GetForPatientAsync(patientId, CancellationToken.None);

        // Assert
        Assert.Equal(["Earlier", "Later"], consultations!.Select(c => c.Diagnosis));
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNull()
    {
        // Act
        var response = await _service.UpdateAsync(999, Request("Flu"), CancellationToken.None);

        // Assert
        Assert.Null(response);
    }

    [Fact]
    public async Task UpdateAsync_ExistingConsultation_ReplacesDetails()
    {
        // Arrange
        var patientId = await SeedPatientAsync();
        var created = await _service.CreateAsync(patientId, Request("Flu"), CancellationToken.None);
        _context.ChangeTracker.Clear();
        var newDate = ConsultationDate.AddDays(1);

        // Act
        var response = await _service.UpdateAsync(created!.Id, Request("Cold", newDate), CancellationToken.None);

        // Assert
        Assert.Equal("Cold", response!.Diagnosis);
        var saved = await _context.Consultations.AsNoTracking().SingleAsync();
        Assert.Equal("Cold", saved.Diagnosis);
        Assert.Equal(newDate, saved.ConsultationDate);
        Assert.Equal(created.CreatedAtUtc, saved.CreatedAtUtc);
    }

    private static ConsultationRequest Request(string diagnosis, DateTime? date = null) => new()
    {
        ConsultationDate = date ?? ConsultationDate,
        Notes = "Patient reports symptoms.",
        Diagnosis = diagnosis
    };

    private async Task<int> SeedPatientAsync()
    {
        var patient = new Patient { Name = "Jane Doe", Age = 30, Gender = Gender.Female };
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();
        return patient.Id;
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
