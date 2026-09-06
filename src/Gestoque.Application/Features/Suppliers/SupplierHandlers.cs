using Gestoque.Application.Common.Interfaces;
using Gestoque.Application.DTOs;
using Gestoque.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gestoque.Application.Features.Suppliers;

public record CreateSupplierCommand(
    string Name,
    string? CnpjCpf,
    string? Phone,
    string? Email,
    string? ContactPerson
) : IRequest<Guid>;

public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, Guid>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public CreateSupplierCommandHandler(IGestoqueDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<Guid> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId 
            ?? throw new InvalidOperationException("Nenhuma empresa ativa selecionada.");

        var supplier = new Supplier
        {
            TenantId = tenantId,
            Name = request.Name,
            CnpjCpf = request.CnpjCpf,
            Phone = request.Phone,
            Email = request.Email,
            ContactPerson = request.ContactPerson,
            IsActive = true
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync(cancellationToken);

        return supplier.Id;
    }
}

public record GetSuppliersQuery : IRequest<List<SupplierDto>>;

public record UpdateSupplierCommand(
    Guid SupplierId,
    string Name,
    string? CnpjCpf,
    string? Phone,
    string? Email,
    string? ContactPerson
) : IRequest;

public class UpdateSupplierCommandHandler : IRequestHandler<UpdateSupplierCommand>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public UpdateSupplierCommandHandler(IGestoqueDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId
            ?? throw new InvalidOperationException("Nenhuma empresa ativa selecionada.");
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(item => item.Id == request.SupplierId && item.TenantId == tenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Fornecedor não encontrado.");

        supplier.Name = request.Name;
        supplier.CnpjCpf = request.CnpjCpf;
        supplier.Phone = request.Phone;
        supplier.Email = request.Email;
        supplier.ContactPerson = request.ContactPerson;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record DeleteSupplierCommand(Guid SupplierId) : IRequest;

public class DeleteSupplierCommandHandler : IRequestHandler<DeleteSupplierCommand>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public DeleteSupplierCommandHandler(IGestoqueDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId
            ?? throw new InvalidOperationException("Nenhuma empresa ativa selecionada.");
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(item => item.Id == request.SupplierId && item.TenantId == tenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Fornecedor não encontrado.");

        var batches = await _context.Batches
            .Where(batch => batch.TenantId == tenantId && batch.SupplierId == request.SupplierId)
            .ToListAsync(cancellationToken);
        foreach (var batch in batches)
            batch.SupplierId = null;

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, List<SupplierDto>>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public GetSuppliersQueryHandler(IGestoqueDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<List<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId
            ?? throw new InvalidOperationException("Nenhuma empresa ativa selecionada.");

        return await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive && s.TenantId == tenantId)
            .OrderBy(s => s.Name)
            .Select(s => new SupplierDto(
                s.Id,
                s.Name,
                s.CnpjCpf,
                s.Phone,
                s.Email,
                s.ContactPerson,
                s.IsActive
            ))
            .ToListAsync(cancellationToken);
    }
}

