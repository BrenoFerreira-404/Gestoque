---
tags:
  - projeto
  - testes
updated: '2026-09-20'
---
# Testes — Gestoque

> Projeto: `tests/Gestoque.Tests` (xUnit + EF Core + SQLite in-memory).

O backlog pedia testes; **ja existem tres suites**. Falta cobertura de Domain isolado, API HTTP e CI.

## `CqrsInventoryTests`

- Cria produto via `CreateProductCommand`
- Duas entradas com validades diferentes
- Saida sem `BatchId` deve respeitar **FEFO** (consome o lote que vence primeiro)
- Atualiza `CurrentStock`

## `ExcelImportTests`

- Importa `29_07_2026 planilha atualizada.xlsx` da raiz
- Espera mais de 50 produtos
- Confere fornecedores e movimentos gerados

## `MultiTenancyIsolationTests`

- Dois tenants no mesmo banco SQLite
- Query filter do `GestoqueDbContext` nao vaza produto de A para B

## Como rodar

```bash
dotnet test tests/Gestoque.Tests/Gestoque.Tests.csproj
```

## Ainda nao coberto (backlog)

- Testes unitarios puros do Domain
- Testes de integracao HTTP da API (WebApplicationFactory)
- CI (GitHub Actions)
