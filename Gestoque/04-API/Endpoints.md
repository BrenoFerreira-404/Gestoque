# Endpoints da API — Gestoque

Base: http://localhost:8080

Fonte de verdade: `src/Gestoque.Api/Program.cs`.
A Web Blazor **nao** usa estas rotas; usa MediatR. Lista completa de handlers: [[01-Projeto/CQRS-MediatR]].

JSON: enums serializados como string (`JsonStringEnumConverter`). CORS: origens em `Cors:Origins` (default `http://localhost:8081`).

## Sem X-Tenant-Id

- `GET /health` — `{ status: ok }`
- `GET /health/ready` — banco acessivel ou 503
- `GET /api/v1/tenants` — lista empresas
- `POST /api/v1/tenants` — body: razaoSocial, nomeFantasia, cnpj, email, telefone → 201 `{ id }`

Nao existem: `GET /api/v1/tenants/{id}`, `/api/v1/empresas`.

## Com X-Tenant-Id (Guid). Sem header: 400

### Dashboard

- `GET /api/v1/dashboard/kpis` — **exige tenant** (cai no middleware)

### Estoque

- `GET /api/v1/estoque?searchTerm=&categoryId=&onlyBelowMinimum=&filterExpiry=`

Nao existe `GET /api/v1/estoque/{produtoId}`.

### Produtos

- `POST /api/v1/produtos` — cria (name, description, categoryId, unit, unitDescription, brand, minimumStock, maximumStock, isUniqueItem, notes)

Nao existem GET/PUT/DELETE de produtos na API.

### Fornecedores

- `GET /api/v1/fornecedores`
- `POST /api/v1/fornecedores`
- `PUT /api/v1/fornecedores/{id}`
- `DELETE /api/v1/fornecedores/{id}`

Nao existe GET por id.

### Movimentacoes

- `GET /api/v1/movimentacoes?productId=&type=&startDate=&endDate=`
- `POST /api/v1/movimentacoes/entradas` — productId, supplierId, batchNumber, expiryDate, quantity, movementDate, brand, documentNumber, notes
- `POST /api/v1/movimentacoes/saidas` — productId, batchId, quantity, reason, movementDate, documentNumber, notes
- `POST /api/v1/movimentacoes/ajustes` — productId, newQuantity, reasonNotes

### Doacoes

- `POST /api/v1/doacoes` — donorName, donorContact, donationDate, receiptNumber, notes, items[]

## Exemplos

```bash
curl http://localhost:8080/health

curl http://localhost:8080/api/v1/tenants

curl http://localhost:8080/api/v1/estoque \
  -H "X-Tenant-Id: SEU-UUID-AQUI"

curl -X POST http://localhost:8080/api/v1/movimentacoes/entradas \
  -H "X-Tenant-Id: SEU-UUID-AQUI" \
  -H "Content-Type: application/json" \
  -d '{"productId":"UUID","quantity":100,"movementDate":"2026-09-20T00:00:00Z"}'
```

## Erros

- `/error` — 500 generico
- Tenant invalido — 400
