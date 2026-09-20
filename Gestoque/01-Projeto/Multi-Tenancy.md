# Multi-Tenancy — Gestoque

## Modelo

Varias empresas no mesmo PostgreSQL. Entidades de negocio implementam `ITenantEntity` (`TenantId`). `Tenant` nao tem TenantId — e o agregado raiz da empresa.

## API

Middleware em `Gestoque.Api/Program.cs`:

- Qualquer path `/api/v1/*` **exceto** `/api/v1/tenants*` exige `X-Tenant-Id` Guid valido.
- Sem header valido: HTTP 400 com mensagem em portugues.
- Com header: `ICurrentTenantService.SetTenantId`.

**Exige tenant:** estoque, produtos, fornecedores, movimentacoes, doacoes, **dashboard/kpis**.

**Nao exige:** `GET/POST /api/v1/tenants`, `/health`, `/health/ready`.

Nao existe rota `/api/v1/empresas`.

```bash
curl http://localhost:8080/api/v1/estoque \
  -H "X-Tenant-Id: 550e8400-e29b-41d4-a716-446655440000"
```

## Web (Blazor)

Nao usa o header. `TenantSessionState` + `ICurrentTenantService` no circuito Blazor. Ultima empresa: `TenantPersistenceService` / `tenantStorage.js`. Ver [[01-Projeto/Interface-Web]].

## Query filters (EF)

Em `GestoqueDbContext`, se `TenantId` da sessao estiver setado, filtra:
Category, Product, Batch, StockMovement, Supplier, Donation, DonationItem.

Se TenantId for null, o filtro nao restringe (usado em listagem de tenants / migrator).

Handlers ainda filtram `TenantId` explicitamente na maioria das queries.

## Empresa matriz

Criada pelo Migrator se nao houver nenhum tenant:

- RazaoSocial: Empresa Matriz - Gestao Geral
- NomeFantasia: Empresa Principal
- Cnpj: 00.000.000/0001-00

## Exclusao de empresa

`DeleteTenantCommand` apaga em cascade os dados daquele tenant (incluindo movimentacoes). Isso e operacao administrativa na Web, nao um "estorno" de estoque. A regra de nao apagar movimentacao vale para o fluxo operacional de estoque.

## Autenticacao

Ainda nao ha JWT. Quem conhece o UUID do tenant acessa a API. Plano: [[05-Decisoes/ADRs]] ADR-002.
