export function getSelectedTenantId() {
    try {
        const value = localStorage.getItem('gestoque_selected_tenant_id');
        return value || null;
    } catch {
        return null;
    }
}

export function setSelectedTenantId(tenantId) {
    try {
        localStorage.setItem('gestoque_selected_tenant_id', tenantId);
    } catch {
        // ignore
    }
}

export function removeSelectedTenantId() {
    try {
        localStorage.removeItem('gestoque_selected_tenant_id');
    } catch {
        // ignore
    }
}