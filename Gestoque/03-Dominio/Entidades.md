# Entidades — Gestoque

> `Gestoque.Domain`. Nomes em ingles no codigo. Base: `BaseEntity` (Id, CreatedAt, UpdatedAt).

`ITenantEntity`: TenantId. `IAuditableEntity`: CreatedBy, UpdatedBy.

Enums: [[03-Dominio/Enums-e-Validade]].

## Tenant

Empresa. Nao e ITenantEntity.

- RazaoSocial, NomeFantasia, Cnpj, Email, Telefone, IsActive, LogoUrl
- Colecoes: Products, Suppliers, StockMovements, Donations

## Product : ITenantEntity

- Name, Description, CategoryId, Unit (`UnitOfMeasure`), UnitDescription, Brand
- MinimumStock, MaximumStock, IsUniqueItem, IsActive, Notes, CurrentStock (desnormalizado)
- `IsBelowMinimumStock` => CurrentStock < MinimumStock
- **Nao existe** codigo de barras nem preco unitario no modelo atual

## Batch : ITenantEntity

Lote para rastreio e validade. `GetExpiryStatus()`.

- BatchNumber, ExpiryDate, InitialQuantity, CurrentQuantity, ReceivedDate, SupplierId, Brand, Notes

## Category : ITenantEntity

- Name, Description, ColorCode
- Sem CRUD na API nem tela dedicada; DTO existe (`CategoryDto`)

## StockMovement : ITenantEntity, IAuditableEntity

- ProductId, BatchId?, MovementType, MovementReason, Quantity, MovementDate, DocumentNumber, Notes, CreatedBy, UpdatedBy

## Supplier : ITenantEntity

- Name, CnpjCpf, Phone, Email, ContactPerson, IsActive

## Donation / DonationItem : ITenantEntity (Donation tambem IAuditableEntity)

Donation: DonorName, DonorContact, DonationDate, ReceiptNumber, Notes, Items, CreatedBy, UpdatedBy

DonationItem: DonationId, ProductId, Quantity, ExpiryDate, Brand, Notes
