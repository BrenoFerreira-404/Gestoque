using Gestoque.Domain.Entities;
using Gestoque.Infrastructure.Data;
using Gestoque.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gestoque.Tests;

public class ExcelImportTests
{
    [Fact]
    public async Task ImportFromExcelAsync_ShouldImportProductsAndSuppliersFromSpreadsheet()
    {
        // Arrange
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var tenantService = new CurrentTenantService();
        var options = new DbContextOptionsBuilder<GestoqueDbContext>()
            .UseSqlite(connection)
            .Options;

        var tenantId = Guid.NewGuid();
        tenantService.SetTenantId(tenantId);

        using (var initialContext = new GestoqueDbContext(options, tenantService))
        {
            await initialContext.Database.EnsureCreatedAsync();

            initialContext.Tenants.Add(new Tenant
            {
                Id = tenantId,
                NomeFantasia = "Empresa Cliente Excel",
                RazaoSocial = "Cliente Oficial Gestão de Estoque Ltda",
                Cnpj = "12.345.678/0001-90"
            });
            await initialContext.SaveChangesAsync();
        }

        var excelPath = @"c:\Projetos_Oficial\Gestoque\29_07_2026 planilha atualizada.xlsx";
        Assert.True(File.Exists(excelPath), "Planilha Excel deve existir no caminho indicado.");

        // Act
        using var context = new GestoqueDbContext(options, tenantService);
        var importer = new ExcelImporterService(context);
        var result = await importer.ImportFromExcelAsync(excelPath, tenantId);

        // Assert
        Assert.True(result.products > 50, $"Esperava mais de 50 produtos importados, importou {result.products}");
        Assert.True(result.suppliers >= 5, $"Esperava pelo menos 5 fornecedores importados, importou {result.suppliers}");
        Assert.True(result.movements > 0, "Esperava lotes e movimentações de saldo inicial.");

        var totalSaidas = await context.StockMovements
            .CountAsync(m => m.TenantId == tenantId && m.MovementType == Gestoque.Domain.Enums.MovementType.Saida);
        Assert.True(totalSaidas > 0, $"Esperava saídas importadas da aba SAÍDA, importou {totalSaidas}");

        // Verificar persistência no banco
        var totalProducts = await context.Products.CountAsync();
        var totalSuppliers = await context.Suppliers.CountAsync();
        var totalBatches = await context.Batches.CountAsync();

        Assert.True(totalProducts > 0);
        Assert.True(totalSuppliers > 0);
        Assert.True(totalBatches > 0);
    }
}

