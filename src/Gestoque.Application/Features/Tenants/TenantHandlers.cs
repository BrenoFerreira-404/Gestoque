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

