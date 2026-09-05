using Gestoque.Domain.Common;

namespace Gestoque.Domain.Entities;

public class Category : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ColorCode { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

