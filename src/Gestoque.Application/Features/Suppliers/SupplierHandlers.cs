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

public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, List<SupplierDto>>
{
    private readonly IGestoqueDbContext _context;

    public GetSuppliersQueryHandler(IGestoqueDbContext context)
    {
        _context = context;
    }

    public async Task<List<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive)
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

