# Gestoque — Contexto para Agentes IA

Sistema de controle de estoque multiempresa em .NET 8.

## Stack

- .NET 8 / C# 12 — Backend
- ASP.NET Core API Minimal — REST API
- Blazor Server — Interface Web
- Entity Framework Core + PostgreSQL — Banco de dados
- Docker Compose — Deploy local

## Arquitetura: Clean Architecture

```
src/
  Gestoque.Domain/        <- Entidades, regras de negocio (SEM dependencias externas)
  Gestoque.Application/   <- Use cases, DTOs, interfaces de servico
  Gestoque.Infrastructure/ <- EF Core, repositorios, servicos concretos
  Gestoque.Api/           <- API Minimal .NET 8, endpoints REST
  Gestoque.Web/           <- Blazor Server (UI)
  Gestoque.Migrator/      <- Roda EF Core migrations e encerra
tests/                    <- Testes unitarios e de integracao
```

## Regras OBRIGATORIAS

1. Domain NAO pode referenciar Application, Infrastructure, Api ou Web
2. Application NAO pode referenciar Infrastructure, Api ou Web
3. TODO endpoint de dados DEVE validar X-Tenant-Id no header
4. Nunca deletar movimentacoes — criar estorno
5. Nunca expor entidades diretamente na API — usar DTOs

## Multi-Tenancy

- Header: `X-Tenant-Id: {UUID da empresa}`
- Obrigatorio em: estoque, produtos, movimentacoes, fornecedores
- Desnecessario em: /health, /api/v1/tenants

## Como Rodar

```bash
# Copiar .env (uma vez apenas)
copy .env.example .env

# Subir tudo
docker compose up --build

# Verificar saude
curl http://localhost:8080/health
```

## God Nodes (classes mais importantes do projeto)

1. Guid — 73 conexoes
2. IGestoqueDbContext — 39 conexoes
3. StockMovement — 35 conexoes
4. ExcelImporterService — 28 conexoes
5. GestoqueDbContext — 27 conexoes
6. Product — 26 conexoes
7. Batch — 26 conexoes
8. ICurrentTenantService — 24 conexoes

## Ferramentas de IA Disponiveis

- **Obsidian MCP** — vault `Gestoque/` via `.mcp.json` (Claude Code), `.cursor/mcp.json` (Cursor) e `kilo.json` (Kilo Code) (`@istrejo/obsidian-mcp`). Tools: `read_note`, `list_notes`, `search_content`, `create_note`, `update_note`. Obsidian nao precisa estar aberto.
- **Graphify** — `graphify god-nodes`, `graphify explain "Classe"`, `graphify path "A" "B"`
- **ai-memory** — http://127.0.0.1:49374 (memoria persistente entre sessoes)

## Vault Obsidian — Notas Importantes

Cofre: pasta `Gestoque/` (MCP `obsidian`). Indice: [[06-Agentes/Mapa-do-Vault]]

- [[06-Agentes/Contexto-Claude]] — contexto detalhado para agentes
- [[02-Tarefas/Em-Progresso]] — LEIA antes de comecar qualquer tarefa
- [[02-Tarefas/Backlog]] — proximas features planejadas
- [[01-Projeto/Arquitetura]] — camadas e Web vs API
- [[01-Projeto/CQRS-MediatR]] — commands e queries
- [[01-Projeto/Interface-Web]] — paginas Blazor
- [[03-Dominio/Entidades]] — entidades do dominio
- [[03-Dominio/Regras-de-Negocio]] — regras reais do codigo
- [[04-API/Endpoints]] — rotas REST realmente mapeadas
- [[05-Decisoes/ADRs]] — decisoes arquiteturais

## Convencoes

- Comentarios: Portugues
- Codigo: Ingles (nomes de classes, metodos, variaveis)
- Namespace raiz: Gestoque.*
- Rotas API: /api/v1/...
- Evite `var` — prefira tipos explicitos

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
