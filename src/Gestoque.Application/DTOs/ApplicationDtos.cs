using Gestoque.Domain.Enums;

namespace Gestoque.Application.DTOs;

public record TenantDto(
    Guid Id,
    string RazaoSocial,
    string NomeFantasia,
    string Cnpj,
    string? Email,
    string? Telefone,
    bool IsActive,
    DateTime CreatedAt
);

public record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    string? ColorCode
);

public record SupplierDto(
    Guid Id,
    string Name,
    string? CnpjCpf,
    string? Phone,
    string? Email,
    string? ContactPerson,
    bool IsActive
);

public record BatchDto(
    Guid Id,
    Guid ProductId,
    string BatchNumber,
    DateTime? ExpiryDate,
    decimal InitialQuantity,
    decimal CurrentQuantity,
    DateTime ReceivedDate,
    string? Brand,
    string? SupplierName,
    ExpiryStatus ExpiryStatus
);

public record PosicaoEstoqueDto(
    Guid ProductId,
    string ProductName,
    string? Description,
    string? CategoryName,
    UnitOfMeasure Unit,
    string? UnitDescription,
    string? Brand,
    decimal CurrentStock,
    decimal MinimumStock,
    bool IsBelowMinimum,
    bool IsUniqueItem,
    DateTime? NextExpiryDate,
    ExpiryStatus ExpiryStatus,
    int ActiveBatchesCount,
    string? Notes
);

public record StockMovementDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? BatchNumber,
    DateTime? ExpiryDate,
    MovementType MovementType,
    MovementReason MovementReason,
    decimal Quantity,
    DateTime MovementDate,
    string? DocumentNumber,
    string? Notes,
    string? CreatedBy
);

public record DashboardKpisDto(
    int TotalProducts,
    int ProductsBelowMinimum,
    int ProductsExpiringSoon,
    int ProductsExpired,
    decimal TotalEntradasMonth,
    decimal TotalSaidasMonth,
    int ActiveTenantsCount
);

