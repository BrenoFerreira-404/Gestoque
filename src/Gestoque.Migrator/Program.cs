using Gestoque.Infrastructure;
using Gestoque.Infrastructure.Data;
using Gestoque.Infrastructure.Services;
using Gestoque.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);

using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();

var context = scope.ServiceProvider.GetRequiredService<GestoqueDbContext>();
await context.Database.MigrateAsync();

if (!await context.Tenants.AnyAsync())
{
    context.Tenants.Add(new Gestoque.Domain.Entities.Tenant
    {
        RazaoSocial = "Empresa Matriz - Gestão Geral",
        NomeFantasia = "Empresa Principal",
        Cnpj = "00.000.000/0001-00",
        Email = "admin@gestoque.com.br",
        Telefone = "(11) 99999-9999",
        IsActive = true
    });

    await context.SaveChangesAsync();
}

var excelFilePath = builder.Configuration["ExcelImport:FilePath"];
if (!string.IsNullOrWhiteSpace(excelFilePath))
{
    var tenant = await context.Tenants
        .OrderBy(t => t.CreatedAt)
        .FirstAsync();

    var hasProducts = await context.Products
        .IgnoreQueryFilters()
        .AnyAsync(p => p.TenantId == tenant.Id);

    var importer = scope.ServiceProvider.GetRequiredService<ExcelImporterService>();
    if (!hasProducts)
    {
        var result = await importer.ImportFromExcelAsync(excelFilePath, tenant.Id);

        Console.WriteLine(
            $"Importação Excel concluída: {result.products} produtos, " +
            $"{result.suppliers} fornecedores e {result.movements} movimentos.");
    }
    else if (!await context.StockMovements
        .IgnoreQueryFilters()
        .AnyAsync(m => m.TenantId == tenant.Id
            && m.MovementType == MovementType.Saida
            && m.MovementReason == MovementReason.ConsumoCozinha
            && m.Notes != null
            && m.Notes.StartsWith("Saída importada da planilha")))
    {
        var movements = await importer.ImportStockOutflowsAsync(excelFilePath, tenant.Id);
        Console.WriteLine($"Saídas da planilha importadas: {movements} movimentos.");
    }
    else
    {
        Console.WriteLine("Importação Excel ignorada — dados da planilha já existem no banco.");
    }
}

Console.WriteLine("Banco de dados migrado e seed inicial concluído.");
