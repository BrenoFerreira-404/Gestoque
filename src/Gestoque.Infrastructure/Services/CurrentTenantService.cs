using Gestoque.Application.Common.Interfaces;

namespace Gestoque.Infrastructure.Services;

public class CurrentTenantService : ICurrentTenantService
{
    public Guid? TenantId { get; private set; }
    public bool IsSuperAdmin { get; private set; } = true; // Por padrão, o criador do sistema é SuperAdmin

    public void SetTenantId(Guid tenantId)
    {
        TenantId = tenantId;
    }

    public void SetSuperAdmin(bool isSuperAdmin)
    {
        IsSuperAdmin = isSuperAdmin;
    }
}

