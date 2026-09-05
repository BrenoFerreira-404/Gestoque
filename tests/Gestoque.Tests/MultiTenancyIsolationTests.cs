using Gestoque.Domain.Entities;
using Gestoque.Domain.Enums;
using Gestoque.Infrastructure.Data;
using Gestoque.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gestoque.Tests;

public class MultiTenancyIsolationTests
{
    [Fact]
    public async Task QueryFilter_ShouldIsolateProductsBetweenDifferentTenants()
    {
        // Arrange: In-memory SQLite connection
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var tenantService = new CurrentTenantService();

        var options = new DbContextOptionsBuilder<GestoqueDbContext>()
            .UseSqlite(connection)
            .Options;

        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();

        // Create schema and seed Tenants
        using (var initialContext = new GestoqueDbContext(options, tenantService))
        {
            await initialContext.Database.EnsureCreatedAsync();

            initialContext.Tenants.Add(new Tenant
            {
                Id = tenantAId,
                NomeFantasia = "Empresa A",
                RazaoSocial = "Empresa A Alimentos Ltda",
                Cnpj = "11.111.111/0001-11"
            });

            initialContext.Tenants.Add(new Tenant
            {
                Id = tenantBId,
                NomeFantasia = "Empresa B",
                RazaoSocial = "Empresa B Alimentos Ltda",
                Cnpj = "22.222.222/0001-22"
            });

            await initialContext.SaveChangesAsync();
        }

        // 1. Inserir dados para Empresa A
        tenantService.SetTenantId(tenantAId);
        using (var contextA = new GestoqueDbContext(options, tenantService))
        {
            contextA.Products.Add(new Product
            {
                TenantId = tenantAId,
                Name = "Arroz Tipo 1 - Empresa A",
                Unit = UnitOfMeasure.KG,
                CurrentStock = 100
            });
            await contextA.SaveChangesAsync();
        }

        // 2. Inserir dados para Empresa B
        tenantService.SetTenantId(tenantBId);
        using (var contextB = new GestoqueDbContext(options, tenantService))
        {
            contextB.Products.Add(new Product
            {
                TenantId = tenantBId,
                Name = "Feijão Preto - Empresa B",
                Unit = UnitOfMeasure.KG,
                CurrentStock = 50
            });
            await contextB.SaveChangesAsync();
        }

        // Act & Assert para Empresa A:
        tenantService.SetTenantId(tenantAId);
        using (var verifyContextA = new GestoqueDbContext(options, tenantService))
        {
            var productsA = await verifyContextA.Products.ToListAsync();
            Assert.Single(productsA);
            Assert.Equal("Arroz Tipo 1 - Empresa A", productsA[0].Name);
            Assert.DoesNotContain(productsA, p => p.Name.Contains("Empresa B"));
        }

        // Act & Assert para Empresa B:
        tenantService.SetTenantId(tenantBId);
        using (var verifyContextB = new GestoqueDbContext(options, tenantService))
        {
            var productsB = await verifyContextB.Products.ToListAsync();
            Assert.Single(productsB);
            Assert.Equal("Feijão Preto - Empresa B", productsB[0].Name);
            Assert.DoesNotContain(productsB, p => p.Name.Contains("Empresa A"));
        }
    }
}

