using Gestoque.Application.Common.Interfaces;
using Gestoque.Application.DTOs;
using Gestoque.Domain.Entities;
using Gestoque.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gestoque.Application.Features.Inventory;

// 1. Registrar Entrada
public record RegistrarEntradaCommand(
    Guid ProductId,
    Guid? SupplierId,
    string? BatchNumber,
    DateTime? ExpiryDate,
    decimal Quantity,
    DateTime MovementDate,
    string? Brand,
    string? DocumentNumber,
    string? Notes
) : IRequest<Guid>;

public class RegistrarEntradaCommandHandler : IRequestHandler<RegistrarEntradaCommand, Guid>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public RegistrarEntradaCommandHandler(IGestoqueDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<Guid> Handle(RegistrarEntradaCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId
            ?? throw new InvalidOperationException("Nenhuma empresa ativa selecionada.");

        if (request.Quantity <= 0)
            throw new ArgumentException("A quantidade deve ser maior que zero.");

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == tenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Produto não encontrado.");

        var batchNumber = string.IsNullOrWhiteSpace(request.BatchNumber)
            ? $"LOTE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}"
            : request.BatchNumber.Trim();

        var movementDate = request.MovementDate.Kind == DateTimeKind.Utc
            ? request.MovementDate
            : DateTime.SpecifyKind(request.MovementDate, DateTimeKind.Utc);

        var batch = new Batch
        {
            TenantId = tenantId,
            ProductId = product.Id,
            BatchNumber = batchNumber,
            ExpiryDate = request.ExpiryDate.HasValue
                ? (request.ExpiryDate.Value.Kind == DateTimeKind.Utc
                    ? request.ExpiryDate
                    : DateTime.SpecifyKind(request.ExpiryDate.Value, DateTimeKind.Utc))
                : null,
            InitialQuantity = request.Quantity,
            CurrentQuantity = request.Quantity,
            ReceivedDate = movementDate,
            SupplierId = request.SupplierId,
            Brand = request.Brand ?? product.Brand,
            Notes = request.Notes
        };

        _context.Batches.Add(batch);

        product.CurrentStock += request.Quantity;
        if (!string.IsNullOrWhiteSpace(request.Brand))
            product.Brand = request.Brand;

        var movement = new StockMovement
        {
            TenantId = tenantId,
            ProductId = product.Id,
            Batch = batch,
            MovementType = MovementType.Entrada,
            MovementReason = MovementReason.CompraFornecedor,
            Quantity = request.Quantity,
            MovementDate = movementDate,
            DocumentNumber = request.DocumentNumber,
            Notes = request.Notes
        };

        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync(cancellationToken);

        return movement.Id;
    }
}

// 2. Registrar Saída
public record RegistrarSaidaCommand(
    Guid ProductId,
    Guid? BatchId,
    decimal Quantity,
    MovementReason Reason,
    DateTime MovementDate,
    string? DocumentNumber,
    string? Notes
) : IRequest<Guid>;

public class RegistrarSaidaCommandHandler : IRequestHandler<RegistrarSaidaCommand, Guid>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public RegistrarSaidaCommandHandler(IGestoqueDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<Guid> Handle(RegistrarSaidaCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId
            ?? throw new InvalidOperationException("Nenhuma empresa ativa selecionada.");

        if (request.Quantity <= 0)
            throw new ArgumentException("A quantidade deve ser maior que zero.");

        var product = await _context.Products
            .Include(p => p.Batches.Where(b => b.TenantId == tenantId && b.CurrentQuantity > 0))
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == tenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Produto não encontrado.");

        if (product.CurrentStock < request.Quantity)
            throw new InvalidOperationException($"Estoque insuficiente. Saldo disponível: {product.CurrentStock}, Solicitado: {request.Quantity}");

        Batch? selectedBatch = null;

        if (request.BatchId.HasValue)
        {
            selectedBatch = product.Batches.FirstOrDefault(b => b.Id == request.BatchId.Value);
            if (selectedBatch != null)
            {
                selectedBatch.CurrentQuantity = Math.Max(0, selectedBatch.CurrentQuantity - request.Quantity);
            }
        }
        else
        {
            // FEFO: Consumir do lote com validade mais próxima
            var batchesOrdered = product.Batches
                .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
                .ToList();

            var remainingToDeduct = request.Quantity;
            foreach (var b in batchesOrdered)
            {
                if (b.CurrentQuantity >= remainingToDeduct)
                {
                    b.CurrentQuantity -= remainingToDeduct;
                    selectedBatch ??= b;
                    remainingToDeduct = 0;
                    break;
                }
                else
                {
                    remainingToDeduct -= b.CurrentQuantity;
                    b.CurrentQuantity = 0;
                    selectedBatch ??= b;
                }
            }
        }

        product.CurrentStock = Math.Max(0, product.CurrentStock - request.Quantity);

        var movementDate = request.MovementDate.Kind == DateTimeKind.Utc
            ? request.MovementDate
            : DateTime.SpecifyKind(request.MovementDate, DateTimeKind.Utc);

        var movement = new StockMovement
        {
            TenantId = tenantId,
            ProductId = product.Id,
            BatchId = selectedBatch?.Id,
            MovementType = MovementType.Saida,
            MovementReason = request.Reason,
            Quantity = request.Quantity,
            MovementDate = movementDate,
            DocumentNumber = request.DocumentNumber,
            Notes = request.Notes
        };

        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync(cancellationToken);

        return movement.Id;
    }
}

// 3. Ajuste de Estoque / Balanço Físico
public record AjustarEstoqueCommand(
    Guid ProductId,
    decimal NewQuantity,
    string ReasonNotes
) : IRequest<Guid>;

public class AjustarEstoqueCommandHandler : IRequestHandler<AjustarEstoqueCommand, Guid>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public AjustarEstoqueCommandHandler(IGestoqueDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<Guid> Handle(AjustarEstoqueCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId
            ?? throw new InvalidOperationException("Nenhuma empresa ativa selecionada.");

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == tenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Produto não encontrado.");

        var diff = request.NewQuantity - product.CurrentStock;
        product.CurrentStock = request.NewQuantity;

        var movement = new StockMovement
        {
            TenantId = tenantId,
            ProductId = product.Id,
            MovementType = MovementType.Ajuste,
            MovementReason = MovementReason.AjusteInventario,
            Quantity = Math.Abs(diff),
            MovementDate = DateTime.UtcNow,
            Notes = $"Ajuste de inventário ({diff:+0.##;-0.##;0}): {request.ReasonNotes}"
        };

        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync(cancellationToken);

        return movement.Id;
    }
}

// 4. Extrato de Movimentações
public record GetMovimentacoesQuery(
    Guid? ProductId = null,
    MovementType? Type = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<List<StockMovementDto>>;

public class GetMovimentacoesQueryHandler : IRequestHandler<GetMovimentacoesQuery, List<StockMovementDto>>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public GetMovimentacoesQueryHandler(IGestoqueDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<List<StockMovementDto>> Handle(GetMovimentacoesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId
            ?? throw new InvalidOperationException("Nenhuma empresa ativa selecionada.");

        var query = _context.StockMovements
            .AsNoTracking()
            .Include(m => m.Product)
            .Include(m => m.Batch)
            .Where(m => m.TenantId == tenantId)
            .AsQueryable();

        if (request.ProductId.HasValue)
            query = query.Where(m => m.ProductId == request.ProductId.Value);

        if (request.Type.HasValue)
            query = query.Where(m => m.MovementType == request.Type.Value);

        if (request.StartDate.HasValue)
        {
            var startDate = request.StartDate.Value.Kind == DateTimeKind.Utc
                ? request.StartDate.Value
                : DateTime.SpecifyKind(request.StartDate.Value, DateTimeKind.Utc);
            query = query.Where(m => m.MovementDate >= startDate);
        }

        if (request.EndDate.HasValue)
        {
            var endDate = request.EndDate.Value.Kind == DateTimeKind.Utc
                ? request.EndDate.Value
                : DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc);
            query = query.Where(m => m.MovementDate <= endDate);
        }

        return await query
            .OrderByDescending(m => m.MovementDate)
            .Select(m => new StockMovementDto(
                m.Id,
                m.ProductId,
                m.Product.Name,
                m.Batch != null ? m.Batch.BatchNumber : null,
                m.Batch != null ? m.Batch.ExpiryDate : null,
                m.MovementType,
                m.MovementReason,
                m.Quantity,
                m.MovementDate,
                m.DocumentNumber,
                m.Notes,
                m.CreatedBy
            ))
            .ToListAsync(cancellationToken);
    }
}

// 5. KPIs do Dashboard
public record GetDashboardKpisQuery : IRequest<DashboardKpisDto>;

public class GetDashboardKpisQueryHandler : IRequestHandler<GetDashboardKpisQuery, DashboardKpisDto>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public GetDashboardKpisQueryHandler(IGestoqueDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<DashboardKpisDto> Handle(GetDashboardKpisQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId
            ?? throw new InvalidOperationException("Nenhuma empresa ativa selecionada.");

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Batches.Where(b => b.TenantId == tenantId && b.CurrentQuantity > 0))
            .Where(p => p.IsActive && p.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var totalProducts = products.Count;
        var belowMinimum = products.Count(p => p.CurrentStock > 0 && p.IsBelowMinimumStock);

        var expiringSoon = 0;
        var expired = 0;

        foreach (var p in products)
        {
            var closestBatch = p.Batches
                .Where(b => b.ExpiryDate.HasValue)
                .OrderBy(b => b.ExpiryDate)
                .FirstOrDefault();

            if (closestBatch != null)
            {
                var status = closestBatch.GetExpiryStatus();
                if (status == ExpiryStatus.ProximoVencimento || status == ExpiryStatus.Critico)
                    expiringSoon++;
                else if (status == ExpiryStatus.Vencido)
                    expired++;
            }
        }

        var registeredMovements = await _context.StockMovements
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .Select(m => new { m.MovementType, m.Quantity })
            .ToListAsync(cancellationToken);

        var totalEntradas = registeredMovements
            .Where(m => m.MovementType == MovementType.Entrada)
            .Sum(m => m.Quantity);

        var totalSaidas = registeredMovements
            .Where(m => m.MovementType == MovementType.Saida)
            .Sum(m => m.Quantity);

        var totalTenants = await _context.Tenants.CountAsync(t => t.IsActive, cancellationToken);

        return new DashboardKpisDto(
            TotalProducts: totalProducts,
            ProductsBelowMinimum: belowMinimum,
            ProductsExpiringSoon: expiringSoon,
            ProductsExpired: expired,
            TotalEntradasMonth: totalEntradas,
            TotalSaidasMonth: totalSaidas,
            ActiveTenantsCount: totalTenants
        );
    }

    private static DateTime ToUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
