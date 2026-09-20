---
tags:
  - projeto
  - cqrs
updated: '2026-09-20'
---
# CQRS e MediatR — Gestoque

> A Application nao e um servico gigante: cada caso de uso e um command ou query do MediatR.

## Decisao

`Gestoque.Application` registra handlers com `AddMediatR` no assembly da propria camada.
A [[04-API/Endpoints|API]] e o [[01-Projeto/Interface-Web|Blazor]] disparam `ISender` / `IMediator` — a Web **nao chama a REST API** em processo.

## Pastas (`Features/`)

- `Tenants/` — empresas
- `Products/` — cadastro e posicao de estoque
- `Inventory/` — entradas, saidas, ajustes, historico, KPIs
- `Suppliers/` — fornecedores
- `Donations/` — doacoes com itens

## Commands e Queries reais

### Empresas
- `CreateTenantCommand`
- `GetTenantsQuery`
- `DeleteTenantCommand` (cascade: itens, movimentacoes, lotes, produtos, doacoes, fornecedores, categorias, tenant)

### Produtos / estoque (consulta)
- `CreateProductCommand`
- `GetPosicaoEstoqueQuery` (filtros: texto, categoria, abaixo do minimo, validade)
- `GetAllProductsQuery` (produtos ativos do tenant)
- `RegistrarSaidaProdutosCommand` (baixa em lote de IDs — usado na UI)

### Inventario
- `RegistrarEntradaCommand` — cria/atualiza lote e soma `CurrentStock`
- `RegistrarSaidaCommand` — impede saldo negativo; **FEFO** se nao vier `BatchId`
- `AjustarEstoqueCommand`
- `GetMovimentacoesQuery` (produto, tipo, periodo)
- `GetDashboardKpisQuery`

### Fornecedores
- `CreateSupplierCommand`, `GetSuppliersQuery`, `UpdateSupplierCommand`, `DeleteSupplierCommand`

### Doacoes
- `RegistrarDoacaoCommand` + `DonationItemInput`

## DTOs

Definidos em `Application/DTOs/ApplicationDtos.cs`. A API e a Web devolvem DTOs, nunca entidades.

## Relacao com a API

Nem todo handler tem endpoint REST. Listagem/edicao de produtos e o dashboard da Web usam MediatR direto. Ver [[04-API/Endpoints]].
