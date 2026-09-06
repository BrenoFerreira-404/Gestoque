using Gestoque.Application.Common.Interfaces;
using Gestoque.Application.DTOs;
using Gestoque.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gestoque.Application.Features.Tenants;

public record CreateTenantCommand(
    string RazaoSocial,
    string NomeFantasia,
    string Cnpj,
    string? Email,
    string? Telefone
) : IRequest<Guid>;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Guid>
{
    private readonly IGestoqueDbContext _context;

    public CreateTenantCommandHandler(IGestoqueDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = new Tenant
        {
            RazaoSocial = request.RazaoSocial,
            NomeFantasia = request.NomeFantasia,
            Cnpj = request.Cnpj,
            Email = request.Email,
            Telefone = request.Telefone,
            IsActive = true
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        return tenant.Id;
    }
}

public record GetTenantsQuery : IRequest<List<TenantDto>>;

public record DeleteTenantCommand(Guid TenantId) : IRequest;

public class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand>
{
    private readonly IGestoqueDbContext _context;

    public DeleteTenantCommandHandler(IGestoqueDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        var donationItems = await _context.DonationItems
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);
        var movements = await _context.StockMovements
            .IgnoreQueryFilters()
            .Where(movement => movement.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);
        var batches = await _context.Batches
            .IgnoreQueryFilters()
            .Where(batch => batch.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);
        var products = await _context.Products
            .IgnoreQueryFilters()
            .Where(product => product.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);
        var donations = await _context.Donations
            .IgnoreQueryFilters()
            .Where(donation => donation.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);
        var suppliers = await _context.Suppliers
            .IgnoreQueryFilters()
            .Where(supplier => supplier.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);
        var categories = await _context.Categories
            .IgnoreQueryFilters()
            .Where(category => category.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.Id == request.TenantId, cancellationToken);

        if (tenant == null)
            throw new InvalidOperationException("Empresa não encontrada.");

        _context.DonationItems.RemoveRange(donationItems);
        _context.StockMovements.RemoveRange(movements);
        _context.Batches.RemoveRange(batches);
        _context.Products.RemoveRange(products);
        _context.Donations.RemoveRange(donations);
        _context.Suppliers.RemoveRange(suppliers);
        _context.Categories.RemoveRange(categories);
        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, List<TenantDto>>
{
    private readonly IGestoqueDbContext _context;

    public GetTenantsQueryHandler(IGestoqueDbContext context)
    {
        _context = context;
    }

    public async Task<List<TenantDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Tenants
            .AsNoTracking()
            .OrderBy(t => t.NomeFantasia)
            .Select(t => new TenantDto(
                t.Id,
                t.RazaoSocial,
                t.NomeFantasia,
                t.Cnpj,
                t.Email,
                t.Telefone,
                t.IsActive,
                t.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}

