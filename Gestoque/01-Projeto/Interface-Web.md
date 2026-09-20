---
tags:
  - projeto
  - ui
  - blazor
updated: '2026-09-20'
---
# Interface Web — Gestoque

> Blazor Server 8 + **Radzen**. Roda em http://localhost:8081. Nao consome a API REST; usa MediatR e EF no mesmo processo.

## Stack da UI

- `Gestoque.Web` — Razor Components, Interactive Server
- Radzen (`AddRadzenComponents`)
- Data Protection persistente em `App_Data/DataProtection-Keys` (evita antiforgery ao recriar container)
- `TenantSessionState` (scoped) + `ITenantPersistenceService` (`wwwroot/js/tenantStorage.js`)

## Menu (`NavMenu.razor`)

### Visao Geral
- `/` — Dashboard / painel de controle (`Home.razor`)

### Estoque
- `/estoque` — Posicao do estoque
- `/entradas` — Entrada de mercadoria
- `/saidas` — Saida e baixas
- `/doacoes` — Doacoes recebidas
- `/movimentacoes` — Extrato de movimentacoes

### Cadastros
- `/produtos` — Produtos
- `/fornecedores` — Fornecedores

### Administracao
- `/empresas` — Tenants
- `/importar-excel` — Importar planilha
- `plano-arquitetura.html` — documento HTML estatico (`wwwroot`)

## Tenant na sessao

1. Usuario escolhe a empresa no layout.
2. `TenantSessionState.SetTenant` grava o Guid em `ICurrentTenantService`.
3. `TenantPersistenceService` guarda o ultimo tenant no browser.
4. Query filters do EF isolam os dados. Ver [[01-Projeto/Multi-Tenancy]].

## KPIs do Home

`GetDashboardKpisQuery` → `DashboardKpisDto`:
- Total de produtos
- Abaixo do minimo
- Proximos ao vencimento (30 dias)
- Vencidos
- Entradas e saidas do mes
- Quantidade de tenants ativos (campo do DTO)

Atalhos: Registrar Entrada / Registrar Saida.

## O que a Web faz e a API ainda nao expoe

- Listar/editar produtos na tela (API so tem `POST /produtos`)
- Persistencia da ultima empresa
- Importacao Excel pela UI
- Exclusao de tenant (`DeleteTenantCommand`)
