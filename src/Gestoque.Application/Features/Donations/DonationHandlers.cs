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

        var donation = new Donation
        {
            TenantId = tenantId,
            DonorName = request.DonorName,
            DonorContact = request.DonorContact,
            DonationDate = request.DonationDate,
            ReceiptNumber = request.ReceiptNumber,
            Notes = request.Notes
        };

        _context.Donations.Add(donation);

        foreach (var itemInput in request.Items)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == itemInput.ProductId, cancellationToken)
                ?? throw new KeyNotFoundException($"Produto {itemInput.ProductId} não encontrado.");

            var donationItem = new DonationItem
            {
                TenantId = tenantId,
                Donation = donation,
                ProductId = product.Id,
                Quantity = itemInput.Quantity,
                ExpiryDate = itemInput.ExpiryDate,
                Brand = itemInput.Brand ?? product.Brand,
                Notes = itemInput.Notes
            };
            _context.DonationItems.Add(donationItem);

            // Create batch
            var batch = new Batch
            {
                TenantId = tenantId,
                ProductId = product.Id,
                BatchNumber = $"DOA-{request.DonationDate:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                ExpiryDate = itemInput.ExpiryDate,
                InitialQuantity = itemInput.Quantity,
                CurrentQuantity = itemInput.Quantity,
                ReceivedDate = request.DonationDate,
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
                MovementDate = request.DonationDate,
                DocumentNumber = request.ReceiptNumber,
                Notes = $"Doação de {request.DonorName}: {itemInput.Notes}"
            };
            _context.StockMovements.Add(movement);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return donation.Id;
    }
}

