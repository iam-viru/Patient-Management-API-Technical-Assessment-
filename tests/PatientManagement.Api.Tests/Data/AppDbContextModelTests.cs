using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PatientManagement.Api.Data;
using PatientManagement.Api.Models.Entities;

namespace PatientManagement.Api.Tests.Data;

public sealed class AppDbContextModelTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;

    public AppDbContextModelTests()
    {
        // SQLite in-memory lives only while the connection is open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Theory]
    [InlineData(typeof(Patient), nameof(Patient.Name), 100)]
    [InlineData(typeof(Patient), nameof(Patient.Gender), 10)]
    [InlineData(typeof(Patient), nameof(Patient.Email), 256)]
    [InlineData(typeof(Patient), nameof(Patient.PhotoUrl), 2048)]
    [InlineData(typeof(Consultation), nameof(Consultation.Notes), 2000)]
    [InlineData(typeof(Consultation), nameof(Consultation.Diagnosis), 500)]
    public void Model_StringProperty_HasExpectedMaxLength(Type entityType, string propertyName, int expectedMaxLength)
    {
        // Arrange
        var property = _context.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;

        // Act
        var maxLength = property.GetMaxLength();

        // Assert
        Assert.Equal(expectedMaxLength, maxLength);
    }

    [Fact]
    public async Task SaveChangesAsync_PatientWithGender_StoresGenderAsString()
    {
        // Arrange
        _context.Patients.Add(new Patient { Name = "Jane Doe", Age = 30, Gender = Gender.Female });

        // Act
        await _context.SaveChangesAsync();

        // Assert
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT Gender FROM Patients";
        var storedValue = await command.ExecuteScalarAsync();
        Assert.Equal("Female", storedValue);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
