# Contexto para agentes — Gestoque

> Leia este arquivo primeiro. Mapa: [[06-Agentes/Mapa-do-Vault]].

## O que e

Estoque multiempresa em .NET 8. Blazor Server (UI) e API Minimal compartilham Application (MediatR). A Web nao chama a API HTTP.

## Onde fica

```
C:\Projetos_Oficial\Gestoque\
  src\Gestoque.Domain|Application|Infrastructure|Api|Web|Migrator
  tests\Gestoque.Tests
  tools\InspectExcel
  Gestoque\                 <- este vault
  docker-compose.yml
  CLAUDE.md
```

## Regras

1. Clean Architecture — sem ciclo Domain/Application para Infra/Api/Web
2. Novo endpoint de dados: validar X-Tenant-Id (exceto /tenants e health)
3. Nao apagar movimentacao no fluxo de estoque — estornar/ajustar. Excecao: DeleteTenant administrativo
4. DTOs na borda, nao entidades
5. Comentarios em portugues, codigo em ingles; evitar `var`
6. Nao commit de .env real; nao adicionar dependencia sem combinar

## Rodar

```bash
copy .env.example .env
docker compose up --build
curl http://localhost:8080/health
dotnet test tests/Gestoque.Tests/Gestoque.Tests.csproj
```

Web :8081 — API :8080. Detalhe: [[01-Projeto/Docker]].

## Ferramentas

- Este vault via MCP `obsidian` (`read_note`, `search_content`, `update_note`, ...). Paths relativos ao cofre, sem `.md`
- Graphify no terminal (`graphify-out/` na raiz do repo)
- ai-memory http://127.0.0.1:49374

## Ler cedo

[[02-Tarefas/Em-Progresso]] · [[02-Tarefas/Backlog]] · [[01-Projeto/Arquitetura]] · [[01-Projeto/CQRS-MediatR]] · [[04-API/Endpoints]] · [[03-Dominio/Regras-de-Negocio]] · [[01-Projeto/Interface-Web]]
