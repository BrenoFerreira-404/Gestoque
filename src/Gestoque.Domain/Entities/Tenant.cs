using Gestoque.Domain.Common;

namespace Gestoque.Domain.Entities;

public class Tenant : BaseEntity
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public bool IsActive { get; set; } = true;
    public string? LogoUrl { get; set; }

    // Navigation collections
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<Donation> Donations { get; set; } = new List<Donation>();
}

