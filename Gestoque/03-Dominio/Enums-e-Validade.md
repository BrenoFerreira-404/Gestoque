---
tags:
  - dominio
  - enums
updated: '2026-09-20'
---
# Enums e validade — Gestoque

> Fonte: `Gestoque.Domain/Enums/DomainEnums.cs` e `Batch.GetExpiryStatus()`.

## MovementType

| Valor | Codigo |
| --- | --- |
| Entrada | 1 |
| Saida | 2 |
| Ajuste | 3 |

## MovementReason

| Valor | Uso tipico |
| --- | --- |
| CompraFornecedor | 1 |
| DoacaoRecebida | 2 |
| ConsumoCozinha | 3 (saidas importadas da planilha) |
| DescarteVencido | 4 |
| AvariaDanificado | 5 |
| AjusteInventario | 6 |
| Outros | 99 |

## UnitOfMeasure

UND, KG, G, L, ML, CX, FD, LATA, PACOTE.

## ExpiryStatus (lote e posicao de estoque)

Calculado pela validade do lote vs data UTC de hoje:

| Status | Criterio | UI |
| --- | --- | --- |
| Normal | > 30 dias (codigo comenta > 60 como verde; o metodo usa > 30 para sair de ProximoVencimento) | Verde |
| ProximoVencimento | <= 30 dias | Amarelo |
| Critico | <= 7 dias | Laranja |
| Vencido | <= 0 dias | Vermelho |

Sem `ExpiryDate`: tratado como Normal.

KPI "proximos ao vencimento" no dashboard: 30 dias (`Home.razor`).

## FEFO na saida

Se `RegistrarSaidaCommand` nao informar `BatchId`, consome lotes ordenados por `ExpiryDate` (nulos por ultimo). Ver [[01-Projeto/CQRS-MediatR]] e [[03-Dominio/Regras-de-Negocio]].
