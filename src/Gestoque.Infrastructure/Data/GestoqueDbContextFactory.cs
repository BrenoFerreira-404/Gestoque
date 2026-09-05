using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Gestoque.Infrastructure.Services;

namespace Gestoque.Infrastructure.Data;

public sealed class GestoqueDbContextFactory : IDesignTimeDbContextFactory<GestoqueDbContext>
{
    public GestoqueDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=gestoque;Username=gestoque;Password=gestoque_dev_password";

        var options = new DbContextOptionsBuilder<GestoqueDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new GestoqueDbContext(options, new CurrentTenantService());
    }
}
