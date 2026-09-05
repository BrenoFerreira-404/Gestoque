namespace Gestoque.Application.Common.Interfaces;

public interface ICurrentTenantService
{
    Guid? TenantId { get; }
    void SetTenantId(Guid tenantId);
    bool IsSuperAdmin { get; }
    void SetSuperAdmin(bool isSuperAdmin);
}

