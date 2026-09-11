using Gestoque.Application;
using Gestoque.Infrastructure;
using Gestoque.Infrastructure.Data;
using Gestoque.Web.Components;
using Gestoque.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// 0. Data Protection com armazenamento persistente (evita erro de antiforgery ao recriar container)
var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("Gestoque.Web");

// 1. Camadas da Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Componentes Radzen Blazor
builder.Services.AddRadzenComponents();

// 3. Gerenciamento de Sessão Multi-Tenant no Blazor
builder.Services.AddScoped<TenantSessionState>();
builder.Services.AddScoped<ITenantPersistenceService, TenantPersistenceService>();

// 4. Blazor Server Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
