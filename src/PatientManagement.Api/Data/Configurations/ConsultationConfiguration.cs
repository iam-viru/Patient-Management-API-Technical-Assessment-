using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientManagement.Api.Models.Entities;

namespace PatientManagement.Api.Data.Configurations;

public class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
{
    public const int NotesMaxLength = 2000;
    public const int DiagnosisMaxLength = 500;

    public void Configure(EntityTypeBuilder<Consultation> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Notes)
            .HasMaxLength(NotesMaxLength);

        builder.Property(c => c.Diagnosis)
            .HasMaxLength(DiagnosisMaxLength);

        builder.HasIndex(c => c.PatientId);
    }
}
