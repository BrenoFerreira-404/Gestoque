# Stack tecnica — Gestoque

## Backend e dados

- .NET 8 / C# 12
- ASP.NET Core 8 — API Minimal (`Gestoque.Api`)
- Blazor Server 8 + **Radzen** (`Gestoque.Web`)
- MediatR — CQRS na Application
- Entity Framework Core 8 + Npgsql
- PostgreSQL 16 (imagem `postgres:16-alpine`)
- ClosedXML — importacao Excel
- SQLite in-memory — testes
- xUnit

## Infraestrutura

- Docker + Docker Compose
- Dockerfiles: `Dockerfile.api`, `Dockerfile.web`, `Dockerfile.migrator`

## Ferramentas de IA

- **Obsidian MCP** — `@istrejo/obsidian-mcp` via `.cursor/mcp.json` e `.mcp.json` (vault `Gestoque/`)
- **Graphify** — grafo AST (`graphify` no terminal)
- **ai-memory** — http://127.0.0.1:8090

## Convencoes

- Namespace: `Gestoque.*`
- API: `/api/v1/...`
- Tenant: header `X-Tenant-Id` (API) ou sessao Blazor
- Comentarios em portugues, identificadores em ingles
- Preferir tipos explicitos (evitar `var`)
- Config: `appsettings.json` + `.env` no Compose
