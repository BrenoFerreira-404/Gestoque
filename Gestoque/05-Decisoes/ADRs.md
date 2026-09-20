# Architecture Decision Records — Gestoque

> Contexto, decisao, consequencias. Novas ADRs entram no fim.

---

## ADR-001: Clean Architecture

**Data:** 2026-09  
**Status:** Aceito

**Contexto:** crescimento, testes e separacao de regras vs infra.

**Decisao:** projetos .NET por camada (Domain, Application, Infrastructure, Api, Web, Migrator).

**Consequencias:**
- (+) Domain/Application testaveis sem HTTP
- (+) Troca de infra sem reescrever regras
- (-) Mais projetos e DI

---

## ADR-002: Multi-tenancy via header (API) e sessao (Web)

**Data:** 2026-09  
**Status:** Aceito (temporario sem auth)

**Decisao:** `X-Tenant-Id` na API; `ICurrentTenantService` + query filters no EF; Blazor guarda o tenant no circuito e no localStorage.

**Consequencias:**
- (+) Simples de testar com curl
- (-) UUID conhecido acessa dados da empresa
- Plano: JWT com tenant claim

---

## ADR-003: Docker Compose local

**Data:** 2026-09  
**Status:** Aceito

**Decisao:** db + migrator + api + web no Compose, Web e API apos migration OK.

---

## ADR-004: Migrator EF separado

**Data:** 2026-09  
**Status:** Aceito

**Decisao:** processo que aplica migrations, seed da matriz e import Excel, depois encerra.

**Consequencias:** Api/Web sem DDL; um servico a mais no compose.

---

## ADR-005: CQRS com MediatR

**Data:** 2026-09  
**Status:** Aceito

**Decisao:** um command/query por caso de uso em `Application/Features`. Sem repositorios alem de `IGestoqueDbContext`.

**Consequencias:** handlers claros; API e Web compartilham a Application; nem todo handler tem REST.

---

## ADR-006: Blazor Server in-process (nao BFF da API)

**Data:** 2026-09  
**Status:** Aceito

**Decisao:** a UI chama MediatR no mesmo processo. A API Minimal e superficie para integracao externa/futura.

**Consequencias:** duas formas de entrar nos mesmos handlers; risco de API ficar atras da UI (hoje produtos so tem POST na API).

---

## ADR-007: Importacao Excel com ClosedXML

**Data:** 2026-09  
**Status:** Aceito

**Decisao:** planilha operacional do cliente (`ESTOQUE`, `ENTRADA ENTREGA`) via `ExcelImporterService`, no Migrator e na tela Admin.

**Consequencias:** onboarding rapido; acoplamento ao layout da planilha; testes dependem do arquivo na raiz.

---

## ADR-008: Radzen na UI

**Data:** 2026-09  
**Status:** Aceito

**Decisao:** componentes Radzen (menu, grid, dialog, notification) no Blazor Server.
