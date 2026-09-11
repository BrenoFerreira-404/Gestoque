using Microsoft.JSInterop;

namespace Gestoque.Web.Services;

public interface ITenantPersistenceService
{
    Task<Guid?> GetLastSelectedTenantIdAsync();
    Task SetLastSelectedTenantIdAsync(Guid tenantId);
    Task ClearLastSelectedTenantIdAsync();
}

public class TenantPersistenceService : ITenantPersistenceService
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public TenantPersistenceService(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./js/tenantStorage.js").AsTask());
    }

    public async Task<Guid?> GetLastSelectedTenantIdAsync()
    {
        try
        {
            var module = await _moduleTask.Value;
            var value = await module.InvokeAsync<string>("getSelectedTenantId");
            return string.IsNullOrEmpty(value) ? null : Guid.Parse(value);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetLastSelectedTenantIdAsync(Guid tenantId)
    {
        try
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("setSelectedTenantId", tenantId.ToString());
        }
        catch
        {
            // ignore
        }
    }

    public async Task ClearLastSelectedTenantIdAsync()
    {
        try
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("removeSelectedTenantId");
        }
        catch
        {
            // ignore
        }
    }
}