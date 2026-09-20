# Arquitetura — Gestoque

## Padrao: Clean Architecture + CQRS

Projetos .NET separados por camada. Casos de uso na Application via MediatR. Detalhe: [[01-Projeto/CQRS-MediatR]].

## Estrutura

```
Gestoque.sln (raiz: C:\Projetos_Oficial\Gestoque)
src/
  Gestoque.Domain/           <- Entidades, enums, BaseEntity, ITenantEntity
  Gestoque.Application/      <- Commands/Queries, DTOs, IGestoqueDbContext, ICurrentTenantService
  Gestoque.Infrastructure/   <- EF Core, query filters, ExcelImporterService
  Gestoque.Api/              <- API Minimal REST (nao e usada pela Web)
  Gestoque.Web/              <- Blazor Server + Radzen (MediatR in-process)
  Gestoque.Migrator/         <- migrations + seed da matriz + import Excel opcional
tests/Gestoque.Tests/        <- xUnit + SQLite in-memory
tools/InspectExcel/          <- inspecao de planilha
plano-arquitetura.html
docker-compose.yml
CLAUDE.md
Gestoque/                    <- este vault Obsidian
```

## Fluxo de dependencias

```
Domain <- Application <- Infrastructure
                      <- Api
                      <- Web
Migrator -> Infrastructure
```

- Domain nao depende de ninguem
- Application depende apenas de Domain
- Infrastructure implementa interfaces da Application
- Api e Web referenciam Application + Infrastructure
- Web **nao** chama HTTP da Api

## Principios

- SOLID e DDD nas entidades (`Product`, `Batch`, `StockMovement`)
- CQRS com MediatR (nao ha camada de repositorio concreta alem do DbContext)
- Multi-tenancy: header na API + `ICurrentTenantService` + global query filters
- DTOs na borda; entidades nao saem na API
- Migrator aplica schema antes de Api/Web

## Superficies de uso

| Superficie | Porta | Como acessa dados |
| --- | --- | --- |
| Blazor | 8081 | MediatR + EF |
| API | 8080 | MediatR + header X-Tenant-Id |
| PostgreSQL | 5432 | volume `gestoque-postgres` |

Ver [[01-Projeto/Interface-Web]], [[04-API/Endpoints]], [[01-Projeto/Docker]].
