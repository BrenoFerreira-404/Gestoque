# Casos de uso — Gestoque

> Handlers em `Gestoque.Application/Features`. A Web usa MediatR. A API expoe um subconjunto — [[04-API/Endpoints]].

## Empresas

| Caso | Command/Query | API | Web |
| --- | --- | --- | --- |
| Listar | GetTenantsQuery | GET /tenants | /empresas, seletor |
| Criar | CreateTenantCommand | POST /tenants | /empresas |
| Excluir (cascade) | DeleteTenantCommand | nao | /empresas |

Nao ha Ativar/Inativar/ObterPorId como handler. `IsActive` existe na entidade.

## Produtos

| Caso | Command/Query | API | Web |
| --- | --- | --- | --- |
| Criar | CreateProductCommand | POST /produtos | /produtos |
| Listar ativos | GetAllProductsQuery | nao | /produtos |
| Posicao / filtros | GetPosicaoEstoqueQuery | GET /estoque | /estoque |
| Baixa por lista de IDs | RegistrarSaidaProdutosCommand | nao | /saidas |

Nao ha UpdateProduct nem InativarProduct. Tela `/produtos` so lista e cria.

## Estoque

| Caso | Command/Query | API | Web |
| --- | --- | --- | --- |
| Entrada (lote) | RegistrarEntradaCommand | POST /movimentacoes/entradas | /entradas |
| Saida (FEFO ou lote) | RegistrarSaidaCommand | POST /movimentacoes/saidas | /saidas |
| Ajuste | AjustarEstoqueCommand | POST /movimentacoes/ajustes | /estoque |
| Historico | GetMovimentacoesQuery | GET /movimentacoes | /movimentacoes |
| KPIs | GetDashboardKpisQuery | GET /dashboard/kpis (com tenant) | / |

## Fornecedores

Create, Get, Update, Delete — API e `/fornecedores`. Sem GET por id; usa a colecao.

## Doacoes

`RegistrarDoacaoCommand` — POST /doacoes e `/doacoes`.

## Importacao

Nao e MediatR: `ExcelImporterService`. Ver [[01-Projeto/Importacao-Excel]].
