using Gestoque.Application.Common.Interfaces;
using Gestoque.Infrastructure.Data;
using Gestoque.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gestoque.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A connection string 'DefaultConnection' não foi configurada.");

        services.AddScoped<CurrentTenantService>();
        services.AddScoped<ICurrentTenantService>(sp => sp.GetRequiredService<CurrentTenantService>());

        // Blazor Server pode renderizar componentes em paralelo no mesmo circuito.
        // Cada handler recebe seu próprio DbContext para evitar comandos concorrentes
        // na mesma conexão do PostgreSQL.
        services.AddDbContext<GestoqueDbContext>(
            (sp, options) => options.UseNpgsql(connectionString),
            contextLifetime: ServiceLifetime.Transient,
            optionsLifetime: ServiceLifetime.Singleton);

        services.AddTransient<IGestoqueDbContext>(sp => sp.GetRequiredService<GestoqueDbContext>());
        services.AddScoped<ExcelImporterService>();

        return services;
    }
}
