using Gestoque.Application;
using Gestoque.Infrastructure;
using Gestoque.Infrastructure.Data;
using Gestoque.Web.Components;
using Gestoque.Web.Services;
using Microsoft.EntityFrameworkCore;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// 1. Camadas da Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Componentes Radzen Blazor
builder.Services.AddRadzenComponents();

// 3. Gerenciamento de Sessão Multi-Tenant no Blazor
builder.Services.AddScoped<TenantSessionState>();

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
