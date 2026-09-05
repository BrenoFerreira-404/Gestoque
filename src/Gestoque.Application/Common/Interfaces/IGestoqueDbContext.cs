using Gestoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestoque.Application.Common.Interfaces;

public interface IGestoqueDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<Batch> Batches { get; }
    DbSet<StockMovement> StockMovements { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<Donation> Donations { get; }
    DbSet<DonationItem> DonationItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

