# Gestoque

Sistema de controle de estoque multiempresa em .NET 8, Blazor Server, API Minimal e PostgreSQL.

## Executar com Docker Compose

1. Copie .env.example para .env e ajuste a senha do PostgreSQL.
2. Suba os serviços:

~~~bash
docker compose up --build
~~~

Acesse:

- Web: http://localhost:8081
- API: http://localhost:8080
- Health check: http://localhost:8080/health

O serviço migration aguarda o PostgreSQL ficar saudável, aplica as migrations do EF Core e cria a empresa matriz inicial. A Web e a API só iniciam depois que ele termina com sucesso.

## API e multi-tenancy

As rotas de dados da empresa exigem o header X-Tenant-Id com o UUID de uma empresa. O cadastro e a consulta de empresas não exigem esse header.

Exemplo:

~~~bash
curl http://localhost:8080/api/v1/estoque \
  -H "X-Tenant-Id: UUID_DA_EMPRESA"
~~~

Para executar apenas a migration novamente:

~~~bash
docker compose run --rm migration
~~~

Os dados ficam persistidos no volume gestoque-postgres.
