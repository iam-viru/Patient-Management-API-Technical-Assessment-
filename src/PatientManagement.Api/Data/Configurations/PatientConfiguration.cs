using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientManagement.Api.Models.Entities;

namespace PatientManagement.Api.Data.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public const int NameMaxLength = 100;
    public const int GenderMaxLength = 10;
    public const int EmailMaxLength = 256;
    public const int PhotoUrlMaxLength = 2048;

    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(NameMaxLength);

        // Stored as readable text ("Male") instead of an int, so the DB is self-explanatory.
        builder.Property(p => p.Gender)
            .HasConversion<string>()
            .HasMaxLength(GenderMaxLength);

        builder.Property(p => p.Email)
            .HasMaxLength(EmailMaxLength);

        builder.Property(p => p.PhotoUrl)
            .HasMaxLength(PhotoUrlMaxLength);

        builder.HasMany(p => p.Consultations)
            .WithOne(c => c.Patient)
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
