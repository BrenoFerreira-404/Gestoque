# Regras de negocio — Gestoque

> O que o codigo realmente faz hoje. Desejos (codigo de barras, JWT, notificacao push) ficam no [[02-Tarefas/Backlog]].

## Multi-tenancy

- Dados de negocio levam TenantId e passam por query filter quando ha tenant na sessao.
- API: X-Tenant-Id obrigatorio fora de `/api/v1/tenants`.
- Web: tenant escolhido na sessao, persistido no browser.

## Estoque

- Saida com quantidade <= 0 e rejeitada.
- Saida nao deixa `CurrentStock` negativo; lanca se saldo insuficiente.
- Sem BatchId na saida: **FEFO** (lote com validade mais proxima primeiro).
- Com BatchId: baixa aquele lote (quantidade do lote e clipada em zero).
- `CurrentStock` no produto e desnormalizado e atualizado nos handlers.
- Ajuste: `AjustarEstoqueCommand` com nova quantidade e nota.
- Operacionalmente nao se "edita" uma movimentacao — cria-se outra (entrada/saida/ajuste). Excecao: exclusao administrativa do tenant apaga o historico daquela empresa.

## Validade

- Status no lote: [[03-Dominio/Enums-e-Validade]].
- Dashboard destaca abaixo do minimo, a vencer em 30 dias e vencidos. Nao ha fila de notificacao/email.

## Produtos

- Sem codigo de barras no modelo.
- Listagens padrao (`GetAllProductsQuery`) so ativos (`IsActive`).
- Nao ha unique constraint de nome documentada no snapshot alem do isolamento por tenant.

## Fornecedores e doacoes

- Fornecedor pode ser inativado (`IsActive`) ou removido via `DeleteSupplierCommand`.
- Doacao gera registro + itens; itens carregam validade/marca opcionais.

## Empresa

- CNPJ e string; nao ha validador de digitos no handler de criacao.
- Matriz e seed do Migrator; `DeleteTenantCommand` consegue apagar qualquer tenant pelo Id (incluindo matriz se chamado).
- Sem autenticacao: UUID do tenant e o segredo de fato na API.

## Auditoria

- `CreatedAt` / `UpdatedAt` em BaseEntity.
- `CreatedBy` / `UpdatedBy` em movimentacao e doacao — ainda nao ha usuario logado para preencher de forma consistente.
