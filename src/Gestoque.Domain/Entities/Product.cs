using Gestoque.Domain.Common;
using Gestoque.Domain.Enums;

namespace Gestoque.Domain.Entities;

public class Product : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public UnitOfMeasure Unit { get; set; } = UnitOfMeasure.UND;
    public string? UnitDescription { get; set; } // e.g. "Caixa com 12", "Fardo com 30"
    public string? Brand { get; set; }
    public decimal MinimumStock { get; set; } = 0;
    public decimal MaximumStock { get; set; } = 0;
    public bool IsUniqueItem { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    // Navigation collections
    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    // Calculated / Denormalized current stock for performance
    public decimal CurrentStock { get; set; } = 0;

    public bool IsBelowMinimumStock => CurrentStock < MinimumStock;
}

