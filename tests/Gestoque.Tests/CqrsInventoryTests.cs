using Gestoque.Application.Features.Inventory;
using Gestoque.Application.Features.Products;
using Gestoque.Domain.Entities;
using Gestoque.Domain.Enums;
using Gestoque.Infrastructure.Data;
using Gestoque.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gestoque.Tests;

public class CqrsInventoryTests
{
    [Fact]
    public async Task RegistrarEntrada_And_RegistrarSaida_ShouldUpdateStock_And_RespectFEFO()
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
                NomeFantasia = "Empresa Teste Cozinha",
                RazaoSocial = "Empresa Teste Cozinha Ltda"
            });

            await initialContext.SaveChangesAsync();
        }

        using var context = new GestoqueDbContext(options, tenantService);

        // 1. Cadastrar Produto
        var createProductHandler = new CreateProductCommandHandler(context, tenantService);
        var productId = await createProductHandler.Handle(new CreateProductCommand(
            Name: "Leite Integral UHT 1L",
            Description: "Caixa 1L",
            CategoryId: null,
            Unit: UnitOfMeasure.L,
            UnitDescription: "Caixa",
            Brand: "Itambé",
            MinimumStock: 20,
            MaximumStock: 100,
            IsUniqueItem: false,
            Notes: null
        ), CancellationToken.None);

        // 2. Lançar 2 Entradas com validades distintas
        var entradaHandler = new RegistrarEntradaCommandHandler(context, tenantService);

        // Lote 1: 50 unidades, vence em 10 dias
        var lote1Expiry = DateTime.UtcNow.AddDays(10);
        await entradaHandler.Handle(new RegistrarEntradaCommand(
            ProductId: productId,
            SupplierId: null,
            BatchNumber: "LOTE-VENCE-CEDO",
            ExpiryDate: lote1Expiry,
            Quantity: 50,
            MovementDate: DateTime.UtcNow,
            Brand: "Itambé",
            DocumentNumber: "NF-001",
            Notes: "Primeira entrega"
        ), CancellationToken.None);

        // Lote 2: 50 unidades, vence em 90 dias
        var lote2Expiry = DateTime.UtcNow.AddDays(90);
        await entradaHandler.Handle(new RegistrarEntradaCommand(
            ProductId: productId,
            SupplierId: null,
            BatchNumber: "LOTE-VENCE-TARDE",
            ExpiryDate: lote2Expiry,
            Quantity: 50,
            MovementDate: DateTime.UtcNow,
            Brand: "Itambé",
            DocumentNumber: "NF-002",
            Notes: "Segunda entrega"
        ), CancellationToken.None);

        // Verificar Saldo Total: 100 unidades
        var product = await context.Products.Include(p => p.Batches).FirstAsync(p => p.Id == productId);
        Assert.Equal(100, product.CurrentStock);
        Assert.Equal(2, product.Batches.Count);

        // 3. Registrar Saída de 30 unidades (sem especificar lote -> FEFO deve abater do LOTE-VENCE-CEDO)
        var saidaHandler = new RegistrarSaidaCommandHandler(context, tenantService);
        await saidaHandler.Handle(new RegistrarSaidaCommand(
            ProductId: productId,
            BatchId: null, // FEFO automático
            Quantity: 30,
            Reason: MovementReason.ConsumoCozinha,
            MovementDate: DateTime.UtcNow,
            DocumentNumber: "REQ-01",
            Notes: "Preparo do café da manhã"
        ), CancellationToken.None);

        // Assert:
        var updatedProduct = await context.Products.Include(p => p.Batches).FirstAsync(p => p.Id == productId);
        Assert.Equal(70, updatedProduct.CurrentStock); // 100 - 30 = 70

        var lote1 = updatedProduct.Batches.First(b => b.BatchNumber == "LOTE-VENCE-CEDO");
        var lote2 = updatedProduct.Batches.First(b => b.BatchNumber == "LOTE-VENCE-TARDE");

        Assert.Equal(20, lote1.CurrentQuantity); // 50 - 30 = 20 (consumido primeiro!)
        Assert.Equal(50, lote2.CurrentQuantity); // 50 intacto

        // 4. Testar Query da Posição do Estoque
        var posicaoHandler = new GetPosicaoEstoqueQueryHandler(context);
        var posicao = await posicaoHandler.Handle(new GetPosicaoEstoqueQuery(), CancellationToken.None);

        Assert.Single(posicao);
        Assert.Equal(70, posicao[0].CurrentStock);
        Assert.Equal(20, posicao[0].MinimumStock);
        Assert.False(posicao[0].IsBelowMinimum);
        Assert.Equal(ExpiryStatus.ProximoVencimento, posicao[0].ExpiryStatus); // Vence em 10 dias!
    }
}

