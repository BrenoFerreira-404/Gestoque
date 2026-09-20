# Docker — Gestoque

## Pre-requisitos

- Docker Desktop
- `.env` copiado de `.env.example` (nao commitar senhas reais)

## Subir

```bash
copy .env.example .env
docker compose up --build
docker compose up --build -d
```

## Servicos (`docker-compose.yml`)

| Servico | Papel | Porta host |
| --- | --- | --- |
| db | PostgreSQL 16 | 5432 (`POSTGRES_PORT`) |
| migration | EF migrate + seed + Excel | sai ao terminar |
| api | API Minimal | 8080 (`API_PORT`) |
| web | Blazor | 8081 (`WEB_PORT`) mapeado para 8080 no container |

## Ordem

1. `db` healthy (`pg_isready`)
2. `migration` aplica migrations, cria empresa matriz se o banco estiver vazio, importa Excel se `ExcelImport__FilePath` estiver setado
3. `api` e `web` so sobem com `service_completed_successfully` do migration

## Excel no Migrator

Compose define:

`ExcelImport__FilePath: /app/29_07_2026 planilha atualizada.xlsx`

Ver [[01-Projeto/Importacao-Excel]].

## Comandos

```bash
docker compose logs -f
docker compose run --rm migration
docker compose down
docker compose down -v   # apaga o volume — perde dados
docker compose ps
curl http://localhost:8080/health
curl http://localhost:8080/health/ready
```

## Volumes

- `gestoque-postgres` — dados persistentes

## Dockerfiles

- `Dockerfile.api`
- `Dockerfile.web`
- `Dockerfile.migrator`
