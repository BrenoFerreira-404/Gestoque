# Dashboard — Gestoque

> Controle de estoque multiempresa | .NET 8 · Blazor Server · API Minimal · PostgreSQL

Indice completo: [[06-Agentes/Mapa-do-Vault]]

## Acesso rapido

### Projeto
- [[01-Projeto/Arquitetura]] — camadas e Web vs API
- [[01-Projeto/Stack]] — tecnologias
- [[01-Projeto/Docker]] — Compose e portas
- [[01-Projeto/Multi-Tenancy]] — header, sessao, query filters
- [[01-Projeto/CQRS-MediatR]] — commands e queries
- [[01-Projeto/Interface-Web]] — paginas Blazor/Radzen
- [[01-Projeto/Importacao-Excel]] — planilha e Migrator
- [[01-Projeto/Testes]] — xUnit existente

### Tarefas
- [[02-Tarefas/Em-Progresso]]
- [[02-Tarefas/Backlog]]
- [[02-Tarefas/Concluido]]

### Dominio
- [[03-Dominio/Entidades]]
- [[03-Dominio/Enums-e-Validade]]
- [[03-Dominio/Casos-de-Uso]]
- [[03-Dominio/Regras-de-Negocio]]

### API e decisoes
- [[04-API/Endpoints]] — rotas reais em Program.cs
- [[05-Decisoes/ADRs]]

### Agentes
- [[06-Agentes/Contexto-Claude]]
- [[06-Agentes/Memoria]]

## Status

- Fase: desenvolvimento com UI operacional e API parcial
- Auth: ainda sem JWT
- Deploy local: Docker Compose

## Servicos

- Web: http://localhost:8081
- API: http://localhost:8080
- Health: http://localhost:8080/health e `/health/ready`

## Para agentes — leia nesta ordem

1. [[06-Agentes/Contexto-Claude]]
2. [[02-Tarefas/Em-Progresso]]
3. [[01-Projeto/Arquitetura]]
4. [[03-Dominio/Regras-de-Negocio]]
