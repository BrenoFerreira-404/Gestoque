using System.Text.Json.Serialization;
using Gestoque.Application;
using Gestoque.Application.Common.Interfaces;
using Gestoque.Application.Features.Donations;
using Gestoque.Application.Features.Inventory;
using Gestoque.Application.Features.Products;
using Gestoque.Application.Features.Suppliers;
using Gestoque.Application.Features.Tenants;
using Gestoque.Domain.Enums;
using Gestoque.Infrastructure;
using Gestoque.Infrastructure.Data;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? ["http://localhost:8081"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseExceptionHandler("/error");
app.UseCors();

// O tenant é informado por chamada para manter o isolamento entre empresas.
app.Use(async (httpContext, next) =>
{
    if (httpContext.Request.Path.StartsWithSegments("/api/v1")
        && !httpContext.Request.Path.StartsWithSegments("/api/v1/tenants"))
    {
        var tenantHeader = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (!Guid.TryParse(tenantHeader, out var tenantId))
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                error = "Informe um X-Tenant-Id válido para acessar os dados da empresa."
            });
            return;
        }

        httpContext.RequestServices.GetRequiredService<ICurrentTenantService>()
            .SetTenantId(tenantId);
    }

    await next();
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/health/ready", async (GestoqueDbContext context, CancellationToken cancellationToken) =>
{
    var canConnect = await context.Database.CanConnectAsync(cancellationToken);
    return canConnect
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

var api = app.MapGroup("/api/v1");

api.MapGet("/tenants", async (ISender sender, CancellationToken cancellationToken) =>
    Results.Ok(await sender.Send(new GetTenantsQuery(), cancellationToken)));

api.MapPost("/tenants", async (
    CreateTenantRequest request,
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var id = await sender.Send(new CreateTenantCommand(
        request.RazaoSocial,
        request.NomeFantasia,
        request.Cnpj,
        request.Email,
        request.Telefone), cancellationToken);

    return Results.Created($"/api/v1/tenants/{id}", new { id });
});

api.MapGet("/dashboard/kpis", async (ISender sender, CancellationToken cancellationToken) =>
    Results.Ok(await sender.Send(new GetDashboardKpisQuery(), cancellationToken)));

api.MapGet("/estoque", async (
    string? searchTerm,
    Guid? categoryId,
    bool? onlyBelowMinimum,
    ExpiryStatus? filterExpiry,
    ISender sender,
    CancellationToken cancellationToken) =>
    Results.Ok(await sender.Send(new GetPosicaoEstoqueQuery(
        searchTerm, categoryId, onlyBelowMinimum, filterExpiry), cancellationToken)));

api.MapPost("/produtos", async (
    CreateProductRequest request,
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var id = await sender.Send(new CreateProductCommand(
        request.Name,
        request.Description,
        request.CategoryId,
        request.Unit,
        request.UnitDescription,
        request.Brand,
        request.MinimumStock,
        request.MaximumStock,
        request.IsUniqueItem,
        request.Notes), cancellationToken);

    return Results.Created($"/api/v1/produtos/{id}", new { id });
});

api.MapGet("/fornecedores", async (ISender sender, CancellationToken cancellationToken) =>
    Results.Ok(await sender.Send(new GetSuppliersQuery(), cancellationToken)));

api.MapPost("/fornecedores", async (
    CreateSupplierRequest request,
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var id = await sender.Send(new CreateSupplierCommand(
        request.Name,
        request.CnpjCpf,
        request.Phone,
        request.Email,
        request.ContactPerson), cancellationToken);

    return Results.Created($"/api/v1/fornecedores/{id}", new { id });
});

api.MapGet("/movimentacoes", async (
    Guid? productId,
    MovementType? type,
    DateTime? startDate,
    DateTime? endDate,
    ISender sender,
    CancellationToken cancellationToken) =>
    Results.Ok(await sender.Send(new GetMovimentacoesQuery(
        productId, type, startDate, endDate), cancellationToken)));

api.MapPost("/movimentacoes/entradas", async (
    RegisterEntryRequest request,
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var id = await sender.Send(new RegistrarEntradaCommand(
        request.ProductId,
        request.SupplierId,
        request.BatchNumber,
        request.ExpiryDate,
        request.Quantity,
        request.MovementDate,
        request.Brand,
        request.DocumentNumber,
        request.Notes), cancellationToken);

    return Results.Created($"/api/v1/movimentacoes/{id}", new { id });
});

api.MapPost("/movimentacoes/saidas", async (
    RegisterExitRequest request,
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var id = await sender.Send(new RegistrarSaidaCommand(
        request.ProductId,
        request.BatchId,
        request.Quantity,
        request.Reason,
        request.MovementDate,
        request.DocumentNumber,
        request.Notes), cancellationToken);

    return Results.Created($"/api/v1/movimentacoes/{id}", new { id });
});

api.MapPost("/movimentacoes/ajustes", async (
    AdjustStockRequest request,
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var id = await sender.Send(new AjustarEstoqueCommand(
        request.ProductId,
        request.NewQuantity,
        request.ReasonNotes), cancellationToken);

    return Results.Created($"/api/v1/movimentacoes/{id}", new { id });
});

api.MapPost("/doacoes", async (
    RegisterDonationRequest request,
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var id = await sender.Send(new RegistrarDoacaoCommand(
        request.DonorName,
        request.DonorContact,
        request.DonationDate,
        request.ReceiptNumber,
        request.Notes,
        request.Items.Select(item => new DonationItemInput(
            item.ProductId,
            item.Quantity,
            item.ExpiryDate,
            item.Brand,
            item.Notes)).ToList()), cancellationToken);

    return Results.Created($"/api/v1/doacoes/{id}", new { id });
});

app.Map("/error", async httpContext =>
{
    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await httpContext.Response.WriteAsJsonAsync(new
    {
        error = "Ocorreu um erro interno na API."
    });
});

app.Run();

public sealed record CreateTenantRequest(
    string RazaoSocial,
    string NomeFantasia,
    string Cnpj,
    string? Email,
    string? Telefone);

public sealed record CreateProductRequest(
    string Name,
    string? Description,
    Guid? CategoryId,
    UnitOfMeasure Unit,
    string? UnitDescription,
    string? Brand,
    decimal MinimumStock,
    decimal MaximumStock,
    bool IsUniqueItem,
    string? Notes);

public sealed record CreateSupplierRequest(
    string Name,
    string? CnpjCpf,
    string? Phone,
    string? Email,
    string? ContactPerson);

public sealed record RegisterEntryRequest(
    Guid ProductId,
    Guid? SupplierId,
    string? BatchNumber,
    DateTime? ExpiryDate,
    decimal Quantity,
    DateTime MovementDate,
    string? Brand,
    string? DocumentNumber,
    string? Notes);

public sealed record RegisterExitRequest(
    Guid ProductId,
    Guid? BatchId,
    decimal Quantity,
    MovementReason Reason,
    DateTime MovementDate,
    string? DocumentNumber,
    string? Notes);

public sealed record AdjustStockRequest(
    Guid ProductId,
    decimal NewQuantity,
    string ReasonNotes);

public sealed record RegisterDonationRequest(
    string DonorName,
    string? DonorContact,
    DateTime DonationDate,
    string? ReceiptNumber,
    string? Notes,
    List<DonationItemRequest> Items);

public sealed record DonationItemRequest(
    Guid ProductId,
    decimal Quantity,
    DateTime? ExpiryDate,
    string? Brand,
    string? Notes);
