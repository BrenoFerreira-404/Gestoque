using Gestoque.Application.Common.Interfaces;
using Gestoque.Application.DTOs;

namespace Gestoque.Web.Services;

public class TenantSessionState
{
    private readonly ICurrentTenantService _currentTenantService;

    public TenantSessionState(ICurrentTenantService currentTenantService)
    {
        _currentTenantService = currentTenantService;
    }

    public Guid? SelectedTenantId => _currentTenantService.TenantId;
    public string SelectedTenantName { get; private set; } = "Carregando...";
    public bool IsSuperAdmin => _currentTenantService.IsSuperAdmin;

    public event Action? OnTenantChanged;

    public void SetTenant(TenantDto tenant)
    {
        _currentTenantService.SetTenantId(tenant.Id);
        SelectedTenantName = tenant.NomeFantasia;
        OnTenantChanged?.Invoke();
    }

    public void SetTenant(Guid tenantId, string tenantName)
    {
        _currentTenantService.SetTenantId(tenantId);
        SelectedTenantName = tenantName;
        OnTenantChanged?.Invoke();
    }
}

