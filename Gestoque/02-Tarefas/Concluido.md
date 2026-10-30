# Concluido — Gestoque

> Historico alinhado ao codigo em 2026-09-20.

## Setup inicial (2026-09)

Clean Architecture .NET 8, Docker Compose (db, migration, api, web), EF + PostgreSQL, multi-tenancy, Migrator com empresa matriz, Dockerfiles.

## Ferramentas de IA (2026-09)

Vault em `Gestoque/`. MCP Cursor: `@istrejo/obsidian-mcp` (`.cursor/mcp.json`). Graphify na raiz (`graphify-out/`). ai-memory em `:49374`. `CLAUDE.md` na raiz.

## Dominio e CQRS (2026-09)

Entidades Tenant, Product, Batch, Category, StockMovement, Supplier, Donation/DonationItem. MediatR em Features. FEFO na saida. Query filters por tenant.

## API Minimal (2026-09)

Health, tenants, kpis, estoque, POST produtos, CRUD fornecedores, movimentacoes (entrada/saida/ajuste), POST doacoes. Ver [[04-API/Endpoints]].

## Web Blazor + Radzen (2026-09)

Paginas: Home, Posicao, Entrada, Saida, Doacoes, Movimentacoes, Produtos, Fornecedores, Empresas, Importar Excel. Seletor de empresa e persistencia da ultima. Ver [[01-Projeto/Interface-Web]].

## Excel (2026-09)

`ExcelImporterService` + Migrator + tela Admin + `tools/InspectExcel`. Ver [[01-Projeto/Importacao-Excel]].

## Testes (2026-09)

CqrsInventoryTests (FEFO), ExcelImportTests, MultiTenancyIsolationTests. Ver [[01-Projeto/Testes]].

## Vault Obsidian completo (2026-09-20)

Notas de UI, CQRS, Excel, testes, enums/validade; Endpoints e regras alinhados ao codigo; ADRs 005–008; [[06-Agentes/Mapa-do-Vault]].
