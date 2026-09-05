using Gestoque.Domain.Common;
using Gestoque.Domain.Enums;

namespace Gestoque.Domain.Entities;

public class Batch : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public decimal InitialQuantity { get; set; }
    public decimal CurrentQuantity { get; set; }
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public string? Brand { get; set; }
    public string? Notes { get; set; }

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public ExpiryStatus GetExpiryStatus()
    {
        if (!ExpiryDate.HasValue)
            return ExpiryStatus.Normal;

        var daysRemaining = (ExpiryDate.Value.Date - DateTime.UtcNow.Date).TotalDays;

        if (daysRemaining <= 0)
            return ExpiryStatus.Vencido;
        if (daysRemaining <= 7)
            return ExpiryStatus.Critico;
        if (daysRemaining <= 30)
            return ExpiryStatus.ProximoVencimento;

        return ExpiryStatus.Normal;
    }
}

