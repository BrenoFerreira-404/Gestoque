using Gestoque.Domain.Common;

namespace Gestoque.Domain.Entities;

public class Donation : BaseEntity, ITenantEntity, IAuditableEntity
{
    public Guid TenantId { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public string? DonorContact { get; set; }
    public DateTime DonationDate { get; set; } = DateTime.UtcNow;
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }

    public ICollection<DonationItem> Items { get; set; } = new List<DonationItem>();

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public class DonationItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid DonationId { get; set; }
    public Donation Donation { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Brand { get; set; }
    public string? Notes { get; set; }
}

