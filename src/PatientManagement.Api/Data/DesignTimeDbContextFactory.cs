using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PatientManagement.Api.Data;

/// <summary>
/// Used only by the dotnet-ef tool so migrations can be generated without a real connection string.
/// No connection is opened; `database update` must be given a connection string explicitly.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer()
            .Options;

        return new AppDbContext(options);
    }
}
