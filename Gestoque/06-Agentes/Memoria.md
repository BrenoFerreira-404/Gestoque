# Memoria de sessoes — Gestoque

## Decisoes recentes

- Web in-process (MediatR), API REST paralela e incompleta vs UI — ADR-006
- FEFO na saida sem lote — [[03-Dominio/Enums-e-Validade]]
- MCP do vault: `@istrejo/obsidian-mcp` no projeto

## Convencoes descobertas

- Nomes de propriedades das entidades em ingles (Name, CurrentStock), Tenant em portugues (RazaoSocial, Cnpj)
- Rotas API em portugues (`/produtos`, `/estoque`) mas resource de empresa e `/tenants`
- Listagens de produto filtram IsActive

## Problemas conhecidos / lacunas

- API sem GET/PUT/DELETE de produtos
- Sem CRUD de Category na API/UI
- Sem JWT
- Regras antigas de codigo de barras nao se aplicam ao modelo
- `DeleteTenantCommand` apaga movimentacoes da empresa

## Ultimas alteracoes

### 2026-09-20 (vault completo)

- Notas novas: CQRS, Interface-Web, Importacao-Excel, Testes, Enums-e-Validade, Mapa-do-Vault
- Endpoints, regras, casos de uso, ADRs, stack, docker, multi-tenancy, entidades, dashboard alinhados ao codigo
- MCP `@istrejo/obsidian-mcp` habilitado no Cursor

### 2026-09-20 (antes)

- Sincronizacao inicial do vault com o repositorio
