using Gestoque.Domain.Common;
using Gestoque.Domain.Enums;

namespace Gestoque.Domain.Entities;

public class StockMovement : BaseEntity, ITenantEntity, IAuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid? BatchId { get; set; }
    public Batch? Batch { get; set; }

    public MovementType MovementType { get; set; }
    public MovementReason MovementReason { get; set; }
    public decimal Quantity { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    public string? DocumentNumber { get; set; } // NF, Recibo, Requisição
    public string? Notes { get; set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

