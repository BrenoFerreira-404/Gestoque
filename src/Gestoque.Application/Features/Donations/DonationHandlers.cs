using Gestoque.Application.Common.Interfaces;
using Gestoque.Domain.Entities;
using Gestoque.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gestoque.Application.Features.Donations;

public record DonationItemInput(
    Guid ProductId,
    decimal Quantity,
    DateTime? ExpiryDate,
    string? Brand,
    string? Notes
);

public record RegistrarDoacaoCommand(
    string DonorName,
    string? DonorContact,
    DateTime DonationDate,
    string? ReceiptNumber,
    string? Notes,
    List<DonationItemInput> Items
) : IRequest<Guid>;

public class RegistrarDoacaoCommandHandler : IRequestHandler<RegistrarDoacaoCommand, Guid>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public RegistrarDoacaoCommandHandler(IGestoqueDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<Guid> Handle(RegistrarDoacaoCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId 
            ?? throw new InvalidOperationException("Nenhuma empresa ativa selecionada.");

        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("A doação deve conter pelo menos um item.");

        var donationDate = request.DonationDate.Kind == DateTimeKind.Utc
            ? request.DonationDate
            : DateTime.SpecifyKind(request.DonationDate, DateTimeKind.Utc);

        var donation = new Donation
        {
            TenantId = tenantId,
            DonorName = request.DonorName,
            DonorContact = request.DonorContact,
            DonationDate = donationDate,
            ReceiptNumber = request.ReceiptNumber,
            Notes = request.Notes
        };

        _context.Donations.Add(donation);

        foreach (var itemInput in request.Items)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == itemInput.ProductId && p.TenantId == tenantId, cancellationToken)
                ?? throw new KeyNotFoundException($"Produto {itemInput.ProductId} não encontrado.");

            var donationItem = new DonationItem
            {
                TenantId = tenantId,
                Donation = donation,
                ProductId = product.Id,
                Quantity = itemInput.Quantity,
                ExpiryDate = itemInput.ExpiryDate.HasValue
                    ? (itemInput.ExpiryDate.Value.Kind == DateTimeKind.Utc
                        ? itemInput.ExpiryDate
                        : DateTime.SpecifyKind(itemInput.ExpiryDate.Value, DateTimeKind.Utc))
                    : null,
                Brand = itemInput.Brand ?? product.Brand,
                Notes = itemInput.Notes
            };
            _context.DonationItems.Add(donationItem);

            // Create batch
            var batch = new Batch
            {
                TenantId = tenantId,
                ProductId = product.Id,
                BatchNumber = $"DOA-{donationDate:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                ExpiryDate = itemInput.ExpiryDate.HasValue
                    ? (itemInput.ExpiryDate.Value.Kind == DateTimeKind.Utc
                        ? itemInput.ExpiryDate
                        : DateTime.SpecifyKind(itemInput.ExpiryDate.Value, DateTimeKind.Utc))
                    : null,
                InitialQuantity = itemInput.Quantity,
                CurrentQuantity = itemInput.Quantity,
                ReceivedDate = donationDate,
                Brand = itemInput.Brand ?? product.Brand,
                Notes = $"Doação de: {request.DonorName}"
            };
            _context.Batches.Add(batch);

            // Update product current stock
            product.CurrentStock += itemInput.Quantity;

            // Stock movement
            var movement = new StockMovement
            {
                TenantId = tenantId,
                ProductId = product.Id,
                Batch = batch,
                MovementType = MovementType.Entrada,
                MovementReason = MovementReason.DoacaoRecebida,
                Quantity = itemInput.Quantity,
                MovementDate = donationDate,
                DocumentNumber = request.ReceiptNumber,
                Notes = $"Doação de {request.DonorName}: {itemInput.Notes}"
            };
            _context.StockMovements.Add(movement);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return donation.Id;
    }
}

