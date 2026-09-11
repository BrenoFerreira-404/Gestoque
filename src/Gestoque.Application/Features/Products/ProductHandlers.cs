using Gestoque.Application.Common.Interfaces;
using Gestoque.Application.DTOs;
using Gestoque.Application.Features.Inventory;
using Gestoque.Domain.Entities;
using Gestoque.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Gestoque.Application.Features.Products;

public record CreateProductCommand(
    string Name,
    string? Description,
    Guid? CategoryId,
    UnitOfMeasure Unit,
    string? UnitDescription,
    string? Brand,
    decimal MinimumStock,
    decimal MaximumStock,
    bool IsUniqueItem,
    string? Notes
) : IRequest<Guid>;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public CreateProductCommandHandler(IGestoqueDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId 
            ?? throw new InvalidOperationException("Nenhuma empresa ativa selecionada.");

        var product = new Product
        {
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            CategoryId = request.CategoryId,
            Unit = request.Unit,
            UnitDescription = request.UnitDescription,
            Brand = request.Brand,
            MinimumStock = request.MinimumStock,
            MaximumStock = request.MaximumStock,
            IsUniqueItem = request.IsUniqueItem,
            Notes = request.Notes,
            CurrentStock = 0,
            IsActive = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}

public record GetPosicaoEstoqueQuery(
    string? SearchTerm = null,
    Guid? CategoryId = null,
    bool? OnlyBelowMinimum = null,
    ExpiryStatus? FilterExpiry = null
) : IRequest<List<PosicaoEstoqueDto>>;

public record RegistrarSaidaProdutosCommand(IReadOnlyCollection<Guid> ProductIds) : IRequest<int>;

public class RegistrarSaidaProdutosCommandHandler : IRequestHandler<RegistrarSaidaProdutosCommand, int>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public RegistrarSaidaProdutosCommandHandler(IGestoqueDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<int> Handle(RegistrarSaidaProdutosCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId
            ?? throw new InvalidOperationException("Nenhuma empresa ativa selecionada.");
        var ids = request.ProductIds.Distinct().ToList();
        var products = await _context.Products
            .Where(product => product.TenantId == tenantId && ids.Contains(product.Id))
            .ToListAsync(cancellationToken);
        var exitHandler = new RegistrarSaidaCommandHandler(_context, _currentTenant);
        var exitedProducts = 0;

        foreach (var product in products.Where(product => product.CurrentStock > 0))
        {
            await exitHandler.Handle(new RegistrarSaidaCommand(
                product.Id,
                BatchId: null,
                Quantity: product.CurrentStock,
                Reason: MovementReason.ConsumoCozinha,
                MovementDate: DateTime.UtcNow,
                DocumentNumber: null,
                Notes: "Saída em lote pelo painel de atenção"), cancellationToken);
            exitedProducts++;
        }

        return exitedProducts;
    }
}

public class GetPosicaoEstoqueQueryHandler : IRequestHandler<GetPosicaoEstoqueQuery, List<PosicaoEstoqueDto>>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public GetPosicaoEstoqueQueryHandler(IGestoqueDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<List<PosicaoEstoqueDto>> Handle(GetPosicaoEstoqueQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId
            ?? throw new InvalidOperationException("Nenhuma empresa ativa selecionada.");

        try
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .Where(p => p.IsActive && p.TenantId == tenantId)
                .Where(p => p.Batches.Any(b => b.CurrentQuantity > 0) || p.StockMovements.Any());

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var search = request.SearchTerm.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(search)
                    || (p.Brand != null && p.Brand.ToLower().Contains(search)));
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == request.CategoryId.Value);
            }

            var products = await query.ToListAsync(cancellationToken);

            var result = products.Select(p =>
            {
                var nextBatch = p.Batches
                    .Where(b => b.ExpiryDate.HasValue)
                    .OrderBy(b => b.ExpiryDate)
                    .FirstOrDefault();

                var nextExpiry = nextBatch?.ExpiryDate;
                var status = nextBatch?.GetExpiryStatus() ?? ExpiryStatus.Normal;

                return new PosicaoEstoqueDto(
                    ProductId: p.Id,
                    ProductName: p.Name,
                    Description: p.Description,
                    CategoryName: p.Category?.Name,
                    Unit: p.Unit,
                    UnitDescription: p.UnitDescription,
                    Brand: p.Brand,
                    CurrentStock: p.CurrentStock,
                    MinimumStock: p.MinimumStock,
                    IsBelowMinimum: p.IsBelowMinimumStock,
                    IsUniqueItem: p.IsUniqueItem,
                    NextExpiryDate: nextExpiry,
                    ExpiryStatus: status,
                    ActiveBatchesCount: p.Batches.Count(b => b.CurrentQuantity > 0),
                    Notes: p.Notes
                );
            });

            if (request.OnlyBelowMinimum == true)
            {
                result = result.Where(r => r.IsBelowMinimum);
            }

            if (request.FilterExpiry.HasValue)
            {
                result = result.Where(r => r.ExpiryStatus == request.FilterExpiry.Value);
            }

            return result.OrderBy(r => r.ProductName).ToList();
        }
        catch (Exception ex) when (ex is DecoderFallbackException || ex is IndexOutOfRangeException || ex is ArgumentOutOfRangeException || ex is ArgumentException)
        {
            return new List<PosicaoEstoqueDto>();
        }
    }
}

public record GetAllProductsQuery(
    string? SearchTerm = null,
    Guid? CategoryId = null
) : IRequest<List<PosicaoEstoqueDto>>;

public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, List<PosicaoEstoqueDto>>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public GetAllProductsQueryHandler(IGestoqueDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<List<PosicaoEstoqueDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId
            ?? throw new InvalidOperationException("Nenhuma empresa ativa selecionada.");

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Batches)
            .Where(p => p.IsActive && p.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) 
                                  || (p.Brand != null && p.Brand.ToLower().Contains(search)));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        var products = await query.ToListAsync(cancellationToken);

        return products.Select(p =>
        {
            var nextBatch = p.Batches
                .Where(b => b.ExpiryDate.HasValue)
                .OrderBy(b => b.ExpiryDate)
                .FirstOrDefault();

            var nextExpiry = nextBatch?.ExpiryDate;
            var status = nextBatch?.GetExpiryStatus() ?? ExpiryStatus.Normal;

            return new PosicaoEstoqueDto(
                ProductId: p.Id,
                ProductName: p.Name,
                Description: p.Description,
                CategoryName: p.Category?.Name,
                Unit: p.Unit,
                UnitDescription: p.UnitDescription,
                Brand: p.Brand,
                CurrentStock: p.CurrentStock,
                MinimumStock: p.MinimumStock,
                IsBelowMinimum: p.IsBelowMinimumStock,
                IsUniqueItem: p.IsUniqueItem,
                NextExpiryDate: nextExpiry,
                ExpiryStatus: status,
                ActiveBatchesCount: p.Batches.Count(b => b.CurrentQuantity > 0),
                Notes: p.Notes
            );
        }).OrderBy(r => r.ProductName).ToList();
    }
}

