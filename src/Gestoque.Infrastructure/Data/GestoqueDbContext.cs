using Gestoque.Application.Common.Interfaces;
using Gestoque.Domain.Common;
using Gestoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Gestoque.Infrastructure.Data;

public class GestoqueDbContext : DbContext, IGestoqueDbContext
{
    private readonly ICurrentTenantService _currentTenantService;

    public GestoqueDbContext(
        DbContextOptions<GestoqueDbContext> options,
        ICurrentTenantService currentTenantService) : base(options)
    {
        _currentTenantService = currentTenantService;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<DonationItem> DonationItems => Set<DonationItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Multi-Tenant Global Query Filters
        modelBuilder.Entity<Category>().HasQueryFilter(e => !_currentTenantService.TenantId.HasValue || e.TenantId == _currentTenantService.TenantId);
        modelBuilder.Entity<Product>().HasQueryFilter(e => !_currentTenantService.TenantId.HasValue || e.TenantId == _currentTenantService.TenantId);
        modelBuilder.Entity<Batch>().HasQueryFilter(e => !_currentTenantService.TenantId.HasValue || e.TenantId == _currentTenantService.TenantId);
        modelBuilder.Entity<StockMovement>().HasQueryFilter(e => !_currentTenantService.TenantId.HasValue || e.TenantId == _currentTenantService.TenantId);
        modelBuilder.Entity<Supplier>().HasQueryFilter(e => !_currentTenantService.TenantId.HasValue || e.TenantId == _currentTenantService.TenantId);
        modelBuilder.Entity<Donation>().HasQueryFilter(e => !_currentTenantService.TenantId.HasValue || e.TenantId == _currentTenantService.TenantId);
        modelBuilder.Entity<DonationItem>().HasQueryFilter(e => !_currentTenantService.TenantId.HasValue || e.TenantId == _currentTenantService.TenantId);

        // Precision configuration for decimals
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        // Relationships
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Batch>()
            .HasOne(b => b.Product)
            .WithMany(p => p.Batches)
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockMovement>()
            .HasOne(m => m.Product)
            .WithMany(p => p.StockMovements)
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockMovement>()
            .HasOne(m => m.Batch)
            .WithMany(b => b.StockMovements)
            .HasForeignKey(m => m.BatchId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DonationItem>()
            .HasOne(di => di.Donation)
            .WithMany(d => d.Items)
            .HasForeignKey(di => di.DonationId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                if (_currentTenantService.TenantId.HasValue)
                {
                    entry.Entity.TenantId = _currentTenantService.TenantId.Value;
                }
            }
        }

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        // O PostgreSQL armazena as datas do domínio como timestamp with time zone.
        // Entradas de formulário e planilhas geralmente chegam sem Kind definido;
        // normalizá-las impede falhas do provider Npgsql.
        foreach (var entry in ChangeTracker.Entries()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            foreach (var property in entry.Properties.Where(p =>
                         p.Metadata.ClrType == typeof(DateTime)
                         || p.Metadata.ClrType == typeof(DateTime?)))
            {
                if (property.CurrentValue is DateTime value)
                {
                    property.CurrentValue = ToUtc(value);
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private static DateTime ToUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
