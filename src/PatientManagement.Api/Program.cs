using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore;

using PatientManagement.Api.Data;
using PatientManagement.Api.ErrorHandling;
using PatientManagement.Api.Extensions;
using PatientManagement.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddStorageAndEmailServices();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();

builder.Services.AddSwaggerDocumentation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

var app = builder.Build();

await app.ApplyMigrationsIfEnabledAsync();

app.UseExceptionHandler();
// Turns empty error responses (e.g. 404 for an unknown route, 405) into ProblemDetails bodies.
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseSwaggerDocumentation();
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

// Exposes the implicit Program class to WebApplicationFactory in the integration tests.
public partial class Program;
