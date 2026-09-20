---
tags:
  - projeto
  - excel
updated: '2026-09-20'
---
# Importacao Excel — Gestoque

> Servico: `Gestoque.Infrastructure/Services/ExcelImporterService.cs` (ClosedXML).

## Quando roda

1. **Migrator** (Docker): se `ExcelImport:FilePath` estiver definido, importa na primeira carga da empresa matriz e pode complementar entradas/saidas diarias. Compose aponta para `/app/29_07_2026 planilha atualizada.xlsx`.
2. **Web** — pagina `/importar-excel` (`ImportarExcel.razor`).
3. **Testes** — `ExcelImportTests` usa a planilha na raiz do repo.

Ferramenta auxiliar: `tools/InspectExcel` (inspeciona abas/colunas).

## Abas esperadas

- `ENTRADA ENTREGA` — nomes de fornecedores (a partir da linha 4, coluna A)
- `ESTOQUE` — produtos e saldos; cabecalho com `ALIMENTO`; opcional secao `PROTEINAS`
- Saidas/entradas diarias — metodos `ImportStockOutflowsAsync` e `ImportStockInflowsAsync` (consumo cozinha, notas prefixadas para nao reimportar)

## Colunas da aba ESTOQUE (apos o cabecalho ALIMENTO)

1. Nome do alimento
2. Saldo atual
3. Validade
4. Marca
5. Estoque minimo
6. Observacoes

Unidade e inferida pelo nome (`ResolveUnit`). Saldo > 0 gera lote `LOTE-MIGRAÇÃO-{linha}` e movimentacao de entrada.

## Retorno

`(int products, int suppliers, int movements)` — contagens do que foi criado na importacao principal.

## Isolamento

Importacao recebe `tenantId` explicito e usa `IgnoreQueryFilters` quando precisa achar registros do tenant alvo.
