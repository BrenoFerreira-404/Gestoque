# Backlog — Gestoque

> Nao iniciado. Ao comecar, mover para [[Em-Progresso]].

## Alta prioridade

- [ ] Autenticacao e autorizacao — ver plano detalhado [[Plano-Login]] (não iniciar até aprovar o documento)
- [ ] Completar API REST no mesmo nivel da Web (GET/PUT produtos, GET tenant por id, GET estoque por produto)
- [ ] Auditoria com usuario real (CreatedBy/UpdatedBy)
- [ ] Notificacoes de estoque baixo / vencimento (hoje so KPI e filtro na tela)

## Media prioridade

- [ ] CRUD de categorias (entidade existe, sem tela/API)
- [ ] Historico de movimentacoes com filtros avancados na UI
- [ ] Exportacao de relatorios em PDF
- [ ] Dashboard com graficos (hoje so cards KPI)
- [ ] App mobile (PWA)

## Baixa prioridade

- [ ] Integracao NF-e
- [ ] Codigo de barras (nao existe no modelo)
- [ ] Impressao de etiquetas

## Debito tecnico

- [ ] Testes unitarios do Domain (handlers ja tem suites — [[01-Projeto/Testes]])
- [ ] Testes de integracao HTTP da API
- [ ] CI/CD (GitHub Actions)
- [ ] OpenAPI/Swagger na API
- [ ] Validacao de CNPJ no CreateTenant
- [ ] Proteger exclusao da empresa matriz
